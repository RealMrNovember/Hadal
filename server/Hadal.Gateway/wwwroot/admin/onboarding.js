// HADAL Overseer — first-session guide funnel (docs/34 §5). Uses the helpers of app.js (el, render, api, fmtNum).
"use strict";

const ONBOARDING_STEPS = {
  ob_reactor2: "Reaktörü Sv. 2 yap",
  ob_oxygen: "İlk üretim binası (Oksijen Tesisi)",
  ob_hero: "İlk kahraman (Lyra)",
  ob_oxygen2: "Oksijen Tesisi Sv. 2",
  ob_reactor3: "Reaktör Sv. 3",
  ob_ring2: "2. halkayı aç",
  ob_drones: "Challenger Hangarı kur",
  ob_drones3: "Challenger Hangarı Sv. 3",
  ob_reactor4: "Reaktör Sv. 4 (ilk ordu)",
  ob_monster: "İlk canavar",
  ob_open: "İlk ödül (çantadan aç)",
  ob_gather: "İlk toplama seferi",
  ob_alliance: "İttifaka katıl",
  ob_alliance_chat: "İttifak sohbetine yaz",
  ob_help: "Yardım al ya da yardım et",
  ob_help_ask: "Yardım iste (eski adım)",
  ob_helped: "Yardım al (eski adım)",
};

const fmtSeconds = (s) => (s >= 3600 ? `${Math.floor(s / 3600)} sa ${Math.round((s % 3600) / 60)} dk` : s >= 60 ? `${Math.floor(s / 60)} dk ${s % 60} sn` : `${s} sn`);

async function onboarding(root, arg) {
  const [cohort, days] = (arg || "new-30").split("-");
  const data = await api(`onboarding/funnel?cohort=${encodeURIComponent(cohort || "new")}&days=${encodeURIComponent(days || "30")}`);
  const pick = (c, d) => (location.hash = `#/onboarding/${c}-${d}`);
  const cohortSelect = el("select", { onchange: (e) => pick(e.target.value, data.days) },
    [["new", "Yeni hesaplar"], ["existing", "Sonradan katılan eski hesaplar"], ["all", "Hepsi"]].map(([k, v]) => el("option", { value: k }, v)));
  cohortSelect.value = data.cohort;
  const daysSelect = el("select", { onchange: (e) => pick(data.cohort, e.target.value) },
    [1, 7, 30, 90, 365].map((d) => el("option", { value: String(d) }, `Son ${d} gün`)));
  daysSelect.value = String(data.days);

  const pct = (n, of) => (of > 0 ? `%${((100 * n) / of).toFixed(1)}` : "—");
  let previous = data.started;
  render(root,
    el("h1", {}, "İlk Oturum Hunisi"),
    el("p", { class: "muted" }, "Rehberin her adımına ulaşan hesap sayısı ve rehber başladıktan sonra o adıma kadar geçen ortanca süre. Hedef: 12. adım (ilk toplama seferi) 15–20 dakikada."),
    el("div", { class: "card" }, el("div", { class: "row" }, el("label", {}, "Grup", cohortSelect), el("label", {}, "Dönem", daysSelect))),
    el("div", { class: "card" },
      el("div", {}, `Başlayan: ${fmtNum(data.started)} · Bitiren: ${fmtNum(data.finished)} (${pct(data.finished, data.started)}) · Atlanan eski hesap: ${fmtNum(data.skipped)}`),
      el("div", {}, `İlk 10 dakikada takılan (en az 1 gün önce başlayıp 10. dakikadan sonra hiç adım almayan, bitirmeyen): ${fmtNum(data.stalledIn10Minutes)} / ${fmtNum(data.oldEnough)} (${pct(data.stalledIn10Minutes, data.oldEnough)})`)),
    el("table", {},
      el("thead", {}, el("tr", {}, ["#", "Adım", "Ulaşan", "Başlayanlara göre", "Önceki adımdan kayıp", "Ortanca süre", "İlk 10 dk içinde"].map((h) => el("th", {}, h)))),
      el("tbody", {}, data.steps.map((s) => {
        const lost = previous - s.players;
        previous = s.players;
        return el("tr", {},
          el("td", {}, s.step),
          el("td", {}, ONBOARDING_STEPS[s.stepId] || s.stepId, el("div", { class: "muted mono" }, s.stepId)),
          el("td", {}, fmtNum(s.players)),
          el("td", {}, `%${s.percent}`),
          el("td", { class: lost > 0 ? "bad" : "" }, lost > 0 ? `-${fmtNum(lost)}` : "0"),
          el("td", {}, s.players > 0 ? fmtSeconds(s.medianSeconds) : "—"),
          el("td", {}, fmtNum(s.within10Minutes)));
      }))));
}
