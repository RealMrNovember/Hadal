// HADAL Overseer — economy ledger (docs/30): signed wallet movements by source/resource and a per-day trend.
"use strict";

const ECONOMY_SOURCE_TR = {
  production: "Üretim",
  upkeep: "Bakım",
  reward: "Ödül",
  capsule_open: "Kapsül açma",
  starting_wallet: "Başlangıç cüzdanı",
  building: "Bina",
  unlock: "Kilit açma",
  research: "Araştırma",
  boost: "Takviye",
  troop: "Asker üretimi",
  heal: "Asker iyileştirme",
  repair: "Acil onarım",
  salvage: "Kurtarma",
  refund: "İade",
  purchase: "Satın alma",
  teleport: "Işınlanma",
  rename: "İsim değiştirme",
  mail_attachment: "Posta eki",
  mail_sent: "Posta gönderme",
  march_loot: "Sefer ganimeti",
  gacha: "Gacha",
  hero: "Kahraman geliştirme",
  dive: "Derinlik seferi",
  plunder: "Baskın",
  shield: "Kalkan",
  alliance: "İttifak",
  alliance_create: "İttifak kurma",
  alliance_gift: "İttifak bağışı",
  alliance_research: "İttifak araştırması",
  beacon: "Fener",
};

const signed = (n) => `${n >= 0 ? "+" : ""}${fmtNum(n)}`;

async function economy(root, arg) {
  const days = parseInt(arg || "30", 10) || 30;
  const data = await api(`economy/flow?days=${encodeURIComponent(days)}`);
  const daysSelect = el("select", { onchange: (e) => (location.hash = `#/economy/${e.target.value}`) },
    [1, 7, 30, 90, 365].map((d) => el("option", { value: String(d) }, `Son ${d} gün`)));
  daysSelect.value = String(data.days);

  render(root,
    el("h1", {}, "Ekonomi Akışı"),
    el("p", { class: "muted" }, "Sunucu cüzdan defteri: her kaynağın kazandırdığı (In) ve götürdüğü (Out) tutar. Üretim/bakım tahmini akışlar; diğerleri kesin işlemlerdir."),
    el("div", { class: "card" }, el("div", { class: "row" }, el("label", {}, "Dönem", daysSelect))),
    el("table", {},
      el("thead", {}, el("tr", {}, ["Kaynak", "Birim", "Giriş", "Çıkış", "Net", "İşlem", "Oyuncu"].map((h) => el("th", {}, h)))),
      el("tbody", {}, data.rows.map((r) => el("tr", {},
        el("td", {}, ECONOMY_SOURCE_TR[r.source] || r.source, el("div", { class: "muted mono" }, r.source)),
        el("td", {}, r.resource),
        el("td", {}, fmtNum(r.in)),
        el("td", { class: r.out > 0 ? "bad" : "" }, fmtNum(r.out)),
        el("td", { class: r.net >= 0 ? "" : "bad" }, signed(r.net)),
        el("td", {}, fmtNum(r.events)),
        el("td", {}, fmtNum(r.players)))))),
    el("h2", {}, "Günlere göre"),
    el("table", {},
      el("thead", {}, el("tr", {}, ["Gün", "Birim", "Giriş", "Çıkış", "Net"].map((h) => el("th", {}, h)))),
      el("tbody", {}, data.daysTrend.map((d) => el("tr", {},
        el("td", {}, d.day),
        el("td", {}, d.resource),
        el("td", {}, fmtNum(d.in)),
        el("td", {}, fmtNum(d.out)),
        el("td", { class: d.net >= 0 ? "" : "bad" }, signed(d.net)))))));
}
