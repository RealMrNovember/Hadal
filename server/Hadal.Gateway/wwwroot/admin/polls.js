// HADAL Overseer — alliance polls and time-slot votes (docs/29 §5). Uses the helpers of app.js (el, render, api, toast, can, fmtDate).
"use strict";

const POLL_PURPOSE_TR = { meeting: "Buluşma", lure_trap: "Yem Kapanı", trench_conquest: "Hendek Fethi", alliance_event: "İttifak etkinliği" };
const POLL_CLOSED_TR = { time: "süre doldu", officer: "subay kapattı", overseer: "Overseer kapattı" };

const fmtSlot = (ms) => new Date(ms).toISOString().replace("T", " ").slice(0, 16) + " UTC";

async function polls(root, arg) {
  const query = decodeURIComponent(arg || "");
  const rows = await api("polls" + (query ? `?q=${encodeURIComponent(query)}` : ""));
  const search = el("input", { placeholder: "İttifak etiketi ya da adı", value: query });
  const go = () => (location.hash = "#/polls/" + encodeURIComponent(search.value.trim()));
  search.addEventListener("keydown", (e) => e.key === "Enter" && go());
  const act = (path, method, done) => api(path, { method }).then(() => { toast(done); route(); }, (e) => toast(e.message, true));

  render(root,
    el("h1", {}, "İttifak Oylamaları"),
    el("p", { class: "muted" }, "Her ittifakın açık ve son kapanan oylamaları (anket ve zaman dilimi seçimi). Kötüye kullanılan bir oylama hemen kapatılabilir (özeti ittifak sohbetine düşer) ya da iz bırakmadan silinebilir. İkisi de denetim kaydına yazılır."),
    el("div", { class: "card" }, el("div", { class: "row" }, el("label", {}, "Ara", search), el("button", { onclick: go }, "Ara"))),
    rows.length === 0 ? el("p", { class: "muted" }, "Oylama yok.") : null,
    el("table", {},
      el("thead", {}, el("tr", {}, ["İttifak", "Tür", "Soru", "Seçenekler (oy)", "Açan", "Durum", ""].map((h) => el("th", {}, h)))),
      el("tbody", {}, rows.map((p) => {
        const open = !p.closedAt;
        const total = p.voters;
        return el("tr", {},
          el("td", {}, `[${p.allianceTag}] ${p.allianceName}`),
          el("td", {}, p.kind === "slots" ? `Zaman · ${POLL_PURPOSE_TR[p.purpose] || p.purpose}` : "Anket",
            el("div", { class: "muted" }, [p.multiple ? "çoklu seçim" : "tek seçim", p.anonymous ? "gizli" : "açık oy"].join(" · "))),
          el("td", {}, p.question || "—"),
          el("td", {}, p.options.map((o, i) => el("div", { class: p.winner === i + 1 ? "ok" : "" },
            `${p.kind === "slots" ? fmtSlot(o.atMs) : o.text} — ${o.votes}`)), el("div", { class: "muted" }, `${total} üye oy verdi`)),
          el("td", {}, p.creatorName, el("div", { class: "muted" }, fmtDate(p.createdAt))),
          el("td", {}, open ? el("span", { class: "pill on" }, "Açık") : el("span", { class: "pill off" }, "Kapandı"),
            el("div", { class: "muted" }, open ? `bitiş ${fmtDate(p.closesAt)}` : `${fmtDate(p.closedAt)} · ${POLL_CLOSED_TR[p.closedBy] || p.closedBy}`)),
          el("td", {}, can("gm") ? [
            open ? el("button", { onclick: () => act(`polls/${p.id}/close`, "POST", "Oylama kapatıldı.") }, "Kapat") : null,
            el("button", { class: "danger", onclick: () => confirm("Bu oylama silinsin mi?") && act(`polls/${p.id}`, "DELETE", "Oylama silindi.") }, "Sil"),
          ] : ""));
      }))));
}
