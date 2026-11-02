'use strict';

const tasks = {
  'cup-insertion': { name: 'Cup insertion', description: 'Insert two cups into their targets among occluding box obstacles: depth and contact alignment.' },
  't-shape-assembly': { name: 'T-shape assembly', description: 'Stack two T-shaped blocks into a stable upright configuration: precise position and orientation.' },
  'wire-loop': { name: 'Wire-loop', description: 'Guide a loop over an L-shaped stand and place it around the base: precision and deliberate placement.' }
};
const cards = [...document.querySelectorAll('.modality[data-mode]')];
const tabs = [...document.querySelectorAll('[role="tab"][data-task]')];
const playAll = document.getElementById('play-all');
const playbackStatus = document.getElementById('playback-status');
let playRequest = 0;

function setPlayButton(playing) {
  if (!playAll) return;
  playAll.replaceChildren(document.createTextNode(playing ? 'Pause all ' : 'Play all '));
  const icon = document.createElement('span');
  icon.setAttribute('aria-hidden', 'true');
  icon.textContent = playing ? 'Ⅱ' : '▶';
  playAll.append(icon);
}

function selectTask(tab) {
  const slug = tab.dataset.task;
  const task = tasks[slug];
  if (!task) return;
  playRequest += 1;
  for (const button of tabs) {
    const selected = button === tab;
    button.setAttribute('aria-selected', String(selected));
    button.tabIndex = selected ? 0 : -1;
  }
  for (const card of cards) {
    const video = card.querySelector('video');
    video.pause();
    video.poster = `assets/images/${card.dataset.mode}-${slug}.jpg`;
    video.querySelector('source').src = `assets/videos/${card.dataset.mode}-${slug}.mp4`;
    video.setAttribute('aria-label', `${card.querySelector('h3').textContent} visualization, ${task.name}`);
    card.querySelector('.recording-link').href = `assets/videos/${card.dataset.mode}-${slug}.mp4`;
    video.load();
  }
  document.getElementById('recordings').setAttribute('aria-labelledby', tab.id);
  document.getElementById('task-description').textContent = task.description;
  playbackStatus.textContent = `${task.name} selected. Four recordings ready.`;
  setPlayButton(false);
}

tabs.forEach((tab, index) => {
  tab.addEventListener('click', () => selectTask(tab));
  tab.addEventListener('keydown', (event) => {
    let next;
    if (event.key === 'ArrowRight') next = (index + 1) % tabs.length;
    if (event.key === 'ArrowLeft') next = (index - 1 + tabs.length) % tabs.length;
    if (event.key === 'Home') next = 0;
    if (event.key === 'End') next = tabs.length - 1;
    if (next === undefined) return;
    event.preventDefault();
    tabs[next].focus();
    selectTask(tabs[next]);
  });
});

if (playAll) {
  const videos = cards.map(card => card.querySelector('video'));
  videos.forEach(video => {
    ['pause', 'ended', 'play'].forEach(type => video.addEventListener(type, () => {
      setPlayButton(videos.some(item => !item.paused && !item.ended));
    }));
  });
  playAll.addEventListener('click', async () => {
    const request = ++playRequest;
    if (videos.some(video => !video.paused && !video.ended)) {
      videos.forEach(video => video.pause());
      playbackStatus.textContent = 'All recordings paused.';
      return;
    }
    // These independent recordings start together, but are not synchronized trials.
    const results = await Promise.allSettled(videos.map(video => {
      video.currentTime = 0;
      return video.play();
    }));
    if (request !== playRequest) return;
    const failed = results.filter(result => result.status === 'rejected').length;
    playbackStatus.textContent = failed
      ? 'Some recordings could not start. Use the individual play buttons or recording links.'
      : 'All four recordings started from the beginning.';
    setPlayButton(videos.some(video => !video.paused && !video.ended));
  });
}

const heroVideo = document.getElementById('hero-video');
const heroToggle = document.getElementById('hero-toggle');
if (heroVideo && heroToggle) {
  const reducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)');
  function updateHeroButton() {
    heroToggle.textContent = heroVideo.paused ? 'Play preview ▷' : 'Pause preview Ⅱ';
    heroToggle.setAttribute('aria-label', heroVideo.paused ? 'Play preview' : 'Pause preview');
  }
  function respectMotionPreference() {
    if (reducedMotion.matches) {
      heroVideo.autoplay = false;
      heroVideo.pause();
    }
    updateHeroButton();
  }
  respectMotionPreference();
  reducedMotion.addEventListener('change', respectMotionPreference);
  heroVideo.addEventListener('play', updateHeroButton);
  heroVideo.addEventListener('pause', updateHeroButton);
  heroToggle.addEventListener('click', async () => {
    if (!heroVideo.paused) heroVideo.pause();
    else {
      try { await heroVideo.play(); }
      catch { heroToggle.textContent = 'Preview unavailable'; }
    }
  });
  if ('IntersectionObserver' in window) {
    new IntersectionObserver(entries => {
      if (!entries[0].isIntersecting) heroVideo.pause();
    }, { threshold: 0 }).observe(heroVideo);
  }
}

const copyButton = document.getElementById('copy-citation');
if (copyButton) copyButton.addEventListener('click', async () => {
  const citation = document.getElementById('citation');
  const status = document.getElementById('copy-status');
  try {
    await navigator.clipboard.writeText(citation.textContent);
    copyButton.textContent = 'Copied ✓';
    status.textContent = 'BibTeX citation copied to clipboard.';
    setTimeout(() => { copyButton.textContent = 'Copy BibTeX ⧉'; }, 2500);
  } catch {
    const range = document.createRange();
    range.selectNodeContents(citation);
    const selection = window.getSelection();
    selection.removeAllRanges();
    selection.addRange(range);
    copyButton.textContent = 'Citation selected';
    status.textContent = 'Copy is unavailable. Citation selected; press Control+C or Command+C to copy.';
  }
});
