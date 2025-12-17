const chartGenre = document.getElementById("chartGenre");
const chartList = document.getElementById("chartList");

function esc(s) {
    return String(s ?? "").replace(/[&<>"']/g, m => ({
        "&": "&amp;",
        "<": "&lt;",
        ">": "&gt;",
        '"': "&quot;",
        "'": "&#39;"
    }[m]));
}

// Genres laden
fetch("/api/genres")
    .then(r => r.json())
    .then(genres => {
        genres.forEach(g => {
            const opt = document.createElement("option");
            opt.value = g;
            opt.textContent = g;
            chartGenre.appendChild(opt);
        });
    })
    .catch(err => {
        console.error("Genres laden fehlgeschlagen:", err);
        chartList.innerHTML = "<div style='margin-top:10px;'>Fehler beim Laden der Genres.</div>";
    });

chartGenre.addEventListener("change", async () => {
    const genre = chartGenre.value;
    if (!genre) return;

    chartList.innerHTML = "<div style='margin-top:10px;'>Lade Charts…</div>";

    try {
        // Erwartet: GET /api/charts?genre=...
        const res = await fetch(`/api/charts?genre=${encodeURIComponent(genre)}`);
        const rows = await res.json();

        if (!rows || rows.length === 0) {
            chartList.innerHTML = "<div style='margin-top:10px;'>Keine Daten für dieses Genre.</div>";
            return;
        }

        chartList.innerHTML = rows.map((r, idx) => `
            <div class="trackCard">
                <img class="cover" src="${esc(r.cover_url || r.coverUrl || "")}" alt="Cover" />
                <div class="meta">
                    <div class="songTitle">${idx + 1}. ${esc(r.title || r.Title || "Unbekannter Titel")}</div>
                    <div class="songArtist">${esc(r.artist || r.Artist || "Unbekannter Künstler")}</div>
                    <div style="margin-top:6px; font-size:12px;">
                        👏 ${esc(r.clap_count ?? r.clapCount ?? 0)}
                        &nbsp; | &nbsp; ▶ ${esc(r.play_count ?? r.playCount ?? 0)}
                        &nbsp; | &nbsp; ⏱ ${esc(r.play_seconds ?? r.playSeconds ?? 0)}s
                    </div>
                </div>
            </div>
        `).join("");

    } catch (err) {
        console.error("Charts laden fehlgeschlagen:", err);
        chartList.innerHTML = "<div style='margin-top:10px;'>Fehler beim Laden der Charts.</div>";
    }
});