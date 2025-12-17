// ========= DOM =========
const genreSelect = document.getElementById("genreSelect");
const audio = document.getElementById("audio");

const playBtn = document.getElementById("playBtn");
const pauseBtn = document.getElementById("pauseBtn");
const startBtn = document.getElementById("startBtn"); // optional

const coverImg = document.getElementById("coverImg");
const songTitle = document.getElementById("songTitle");
const songArtist = document.getElementById("songArtist");
const artistLink = document.getElementById("artistLink");

const clapBtn = document.getElementById("clapBtn");
const volumeSlider = document.getElementById("volumeSlider");

const eqLow = document.getElementById("eqLow");
const eqMid = document.getElementById("eqMid");
const eqHigh = document.getElementById("eqHigh");

// ========= STATE =========
let tracks = [];
let currentIndex = 0;
let currentTrack = null;

// ========= WebAudio EQ =========
let audioCtx = null, sourceNode = null, lowFilter = null, midFilter = null, highFilter = null;

function ensureAudioGraph() {
    if (audioCtx) return;

    audioCtx = new (window.AudioContext || window.webkitAudioContext)();
    sourceNode = audioCtx.createMediaElementSource(audio);

    lowFilter = audioCtx.createBiquadFilter();
    lowFilter.type = "lowshelf";
    lowFilter.frequency.value = 120;

    midFilter = audioCtx.createBiquadFilter();
    midFilter.type = "peaking";
    midFilter.frequency.value = 1000;
    midFilter.Q.value = 1;

    highFilter = audioCtx.createBiquadFilter();
    highFilter.type = "highshelf";
    highFilter.frequency.value = 6000;

    sourceNode
        .connect(lowFilter)
        .connect(midFilter)
        .connect(highFilter)
        .connect(audioCtx.destination);

    setEQ();
}

function setEQ() {
    if (!audioCtx) return;
    lowFilter.gain.value = Number(eqLow?.value ?? 0);
    midFilter.gain.value = Number(eqMid?.value ?? 0);
    highFilter.gain.value = Number(eqHigh?.value ?? 0);
}

eqLow?.addEventListener("input", setEQ);
eqMid?.addEventListener("input", setEQ);
eqHigh?.addEventListener("input", setEQ);

// ========= Volume =========
if (volumeSlider) {
    audio.volume = Number(volumeSlider.value || 0.9);
    volumeSlider.addEventListener("input", () => {
        audio.volume = Number(volumeSlider.value);
    });
}

// ========= Helpers =========
function normalizeTrack(t) {
    return {
        id: t.id ?? t.Id,
        title: t.title ?? t.Title ?? "Unbekannter Titel",
        artist: t.artist ?? t.Artist ?? "Unbekannter Künstler",
        genre: t.genre ?? t.Genre ?? "",
        url: t.url ?? t.Url,
        coverUrl: t.cover_url ?? t.coverUrl ?? t.CoverUrl ?? "",
        artistUrl: t.artist_url ?? t.artistUrl ?? t.ArtistUrl ?? "",
        clapCount: t.clap_count ?? t.clapCount ?? t.ClapCount ?? 0,
    };
}

function setTrackUI(track) {
    songTitle.textContent = track.title;
    songArtist.textContent = track.artist;

    if (track.coverUrl) {
        coverImg.src = track.coverUrl;
        coverImg.style.display = "block";
    } else {
        coverImg.removeAttribute("src");
        coverImg.style.display = "none";
    }

    if (track.artistUrl) {
        artistLink.href = track.artistUrl;
        artistLink.style.pointerEvents = "auto";
        artistLink.style.opacity = "1";
    } else {
        artistLink.href = "#";
        artistLink.style.pointerEvents = "none";
        artistLink.style.opacity = "0.5";
    }
}

// Minimal bevorzugen: Clap-Songs leicht häufiger wählen
function weightedPick(list) {
    if (!list.length) return 0;

    const weights = list.map(t => 1 + 0.05 * (Number(t.clapCount) || 0));
    const sum = weights.reduce((a, b) => a + b, 0);

    let r = Math.random() * sum;
    for (let i = 0; i < weights.length; i++) {
        r -= weights[i];
        if (r <= 0) return i;
    }
    return 0;
}

async function safePlay() {
    ensureAudioGraph();
    if (audioCtx && audioCtx.state === "suspended") await audioCtx.resume();
    await audio.play();
}

async function playCurrent() {
    if (!tracks.length) return;

    currentTrack = tracks[currentIndex];
    setTrackUI(currentTrack);

    audio.src = currentTrack.url;
    try {
        await safePlay();
    } catch (e) {
        console.warn("Playback blocked. Click Start/Play.", e);
    }
}

function pickNextIndex() {
    // Wenn du lieber "normale Reihenfolge" willst, nimm:
    // currentIndex = (currentIndex + 1) % tracks.length;
    currentIndex = weightedPick(tracks);
}

// ========= Load Genres =========
fetch("/api/genres")
    .then(r => r.json())
    .then(genres => {
        genres.forEach(g => {
            const opt = document.createElement("option");
            opt.value = g;
            opt.textContent = g;
            genreSelect.appendChild(opt);
        });
    })
    .catch(err => console.error("genres load failed", err));

// ========= Load Tracks =========
async function loadTracksForGenre(genre) {
    // 1) Versuch: /api/tracks?genre=...
    try {
        const res = await fetch(`/api/tracks?genre=${encodeURIComponent(genre)}`);
        if (res.ok) {
            const data = (await res.json()).map(normalizeTrack);
            const filtered = data.filter(t => !t.genre || t.genre === genre);
            return filtered.length ? filtered : data;
        }
    } catch { }

    // 2) Fallback: /api/tracks und hier filtern
    const res2 = await fetch("/api/tracks");
    const all = (await res2.json()).map(normalizeTrack);
    return all.filter(t => t.genre === genre);
}

genreSelect.addEventListener("change", async () => {
    const genre = genreSelect.value;
    if (!genre) return;

    tracks = await loadTracksForGenre(genre);

    // Start sofort mit erstem / gewichtetem Track
    currentIndex = weightedPick(tracks);
    playCurrent();
});

// Loop
audio.addEventListener("ended", () => {
    pickNextIndex();
    playCurrent();
});

// Play/Pause Buttons
playBtn?.addEventListener("click", () => safePlay());
pauseBtn?.addEventListener("click", () => audio.pause());

// Optionaler Start-Button (hilft gegen Autoplay Block)
startBtn?.addEventListener("click", async () => {
    if (!genreSelect.value) {
        const first = [...genreSelect.options].find(o => o.value);
        if (first) genreSelect.value = first.value;
    }
    const genre = genreSelect.value;
    if (!genre) return;

    if (!tracks.length) tracks = await loadTracksForGenre(genre);
    if (!tracks.length) return;

    if (audioCtx && audioCtx.state === "suspended") await audioCtx.resume();
    if (!currentTrack) {
        currentIndex = weightedPick(tracks);
        await playCurrent();
    } else {
        await safePlay();
    }
});

// ========= Clap (nur 1x pro Song pro Tag pro Gerät) =========
function clapKey(trackId) {
    const day = new Date().toISOString().slice(0, 10);
    return `clap:${trackId}:${day}`;
}

clapBtn?.addEventListener("click", async () => {
    if (!currentTrack?.id) return;

    const key = clapKey(currentTrack.id);
    if (localStorage.getItem(key)) return;

    localStorage.setItem(key, "1");

    // optional: backend
    try {
        await fetch("/api/stats/clap", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ trackId: currentTrack.id })
        });
    } catch { }

    clapBtn.textContent = "👏✓";
    setTimeout(() => (clapBtn.textContent = "👏"), 700);
});