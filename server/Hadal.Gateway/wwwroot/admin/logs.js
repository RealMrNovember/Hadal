// HADAL Overseer â€” client crash/error reports (docs/22). Uses app.js helpers (el, render, api, fmtDate, fmtNum, toast).
"use strict";

async function logs(root, arg) {
  const [level, days] = (arg || "all-7").split("-");
  const span = [1, 7, 30, 90].includes(Number(days)) ? days : "7";
  const levelFilter = ["error", "exception"].includes(level) ? level : "all";
  const data = await api(`logs/client?days=${encodeURIComponent(span)}${levelFilter === "all" ? "" : `&level=${encodeURIComponent(levelFilter)}`}`);

  const pick = (l, d) => (location.hash = `#/logs/${l}-${d}`);
  const levelSelect = el("select", { onchange: (e) => pick(e.target.value, span) },
    [["all", "TÃ¼mÃ¼"], ["error", "Hata"], ["exception", "Ä°stisna"]].map(([k, v]) => el("option", { value: k }, v)));
  levelSelect.value = levelFilter;
  const daysSelect = el("select", { onchange: (e) => pick(levelFilter, e.target.value) },
    [1, 7, 30, 90].map((d) => el("option", { value: String(d) }, `Son ${d} gÃ¼n`)));
  daysSelect.value = String(span);

  const search = el("input", { type: "search", placeholder: "Mesajda araâ€¦" });
  const detail = el("pre", { class: "card muted" }, "Bir satÄ±ra tÄ±klayÄ±nca kaydÄ±n ayrÄ±ntÄ±sÄ± burada gÃ¶rÃ¼nÃ¼r.");
  const tbody = el("tbody");

  const rowFor = (entry) => {
    const row = el("tr", {
      class: "log-row",
      onclick: () => {
        detail.textContent =
          `${entry.at} â€” ${entry.level}\nOyuncu: ${entry.playerId}\nSÃ¼rÃ¼m: ${entry.clientVersion} â€” ${entry.platform} â€” ${entry.device}\n\n${entry.message}\n\n${entry.stack || "(stack yok)"}`;
      },
    },
      el("td", {}, fmtDate(entry.at)),
      el("td", { class: "mono" }, (entry.playerId || "").slice(0, 8)),
      el("td", {}, el("span", { class: "pill " + (entry.level === "exception" ? "off" : "on") }, entry.level)),
      el("td", {}, (entry.message || "").slice(0, 160)),
      el("td", {}, entry.clientVersion || "â€”"));
    return row;
  };

  const applyFilter = () => {
    const needle = search.value.trim().toLowerCase();
    tbody.replaceChildren(...data.filter((e) => !needle || (e.message || "").toLowerCase().includes(needle)).map(rowFor));
  };
  search.addEventListener("input", applyFilter);

  render(root,
    el("h1", {}, "Ä°stemci HatalarÄ±"),
    el("p", { class: "muted" }, "Oyuncu istemcilerinden gelen hata ve Ã§Ã¶kme kayÄ±tlarÄ±. En yeni baÅŸta, son 200 kayÄ±t."),
    el("div", { class: "card" }, el("div", { class: "row" },
      el("label", {}, "Seviye", levelSelect),
      el("label", {}, "DÃ¶nem", daysSelect),
      el("label", {}, "Ara", search))),
    el("table", {},
      el("thead", {}, el("tr", {}, ["Zaman", "Oyuncu", "Seviye", "Mesaj", "SÃ¼rÃ¼m"].map((h) => el("th", {}, h)))),
      tbody),
    detail);
  applyFilter();
}
