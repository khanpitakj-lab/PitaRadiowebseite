const genreSelect = document.getElementById("genreSelect");
const audio = document.getElementById("audio");
const playBtn = document.getElementById("playBtn");
const pauseBtn = document.getElementById("pauseBtn");
const nowPlaying = document.getElementById("nowPlaying");

let tracks = [];
let currentIndex = 0;

// Genres laden
fetch("/api/genres")
    .then(r => r.json())
    .then(genres => {
        genres.forEach(g => {
            const opt = document.createElement("option");
            opt.value = g;
            opt.textContent = g;
            genreSelect.appendChild(opt);
        });
    });

// Genre auswählen
genreSelect.addEventListener("change", () => {
    fetch("/api/tracks")
        .then(r => r.json())
        .then(allTracks => {
            tracks = allTracks.filter(t => t.genre === genreSelect.value);
            currentIndex = 0;
            playCurrent();
        });
});

function playCurrent() {
    if (tracks.length === 0) return;

    const track = tracks[currentIndex];
    audio.src = track.url;
    audio.play();
    nowPlaying.textContent = `${track.artist} – ${track.title}`;
}

audio.addEventListener("ended", () => {
    currentIndex = (currentIndex + 1) % tracks.length;
    playCurrent();
});

playBtn.onclick = () => audio.play();
pauseBtn.onclick = () => audio.pause();