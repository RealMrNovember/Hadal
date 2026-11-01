// HADAL Overseer — Events (docs/29 §6). Uses the helpers of app.js (el, render, api, toast, can, fmtDate, fmtNum).
"use strict";

const EVENT_TEMPLATES = {
  points: "Puan yarışması (bireysel)",
  alliance: "İttifak yarışması",
  login: "Giriş ödülü (günlük)",
  mobilization: "Seferberlik (ittifak görev havuzu)",
  alliance_duel: "İttifak Düellosu (haftalık 1'e 1)",
  trench_conquest: "Hendek Fethi (ittifak ligi, 3 hendek)",
  lure_trap: "Yem Kapanı (ittifak kapanı, Yutucu)",
};

// Mobilization task kinds (MobilizationRules.Kinds) and the duel's six day themes (AllianceDuelRules).
const MOBILIZATION_KINDS = {
  gather: "Kaynak toplama", train: "Asker eğitimi (her kademe)", research: "Araştırma seviyesi", build_power: "İnşaat gücü",
  monster_kill: "Yaratık yenme", heal: "Asker iyileştirme", speedup_minutes: "Hızlandırma (dakika)", hero_level: "Kahraman seviyesi",
  help: "İttifak yardımı", dive: "Derinlik Haritası aşaması",
};
const MOBILIZATION_TIERS = ["", "yeşil", "mavi", "mor", "altın"];
const DUEL_DAYS = [
  [{ tr: "Toplama Günü", en: "Gathering Day" }, [{ action: "gather", points: 1, per: 1000 }]],
  [{ tr: "Üs Büyütme", en: "Base Growth" }, [{ action: "build_power", points: 2, per: 1 }, { action: "speedup_minutes", points: 1, per: 1 }]],
  [{ tr: "Araştırma", en: "Research" }, [{ action: "research", points: 300, per: 1 }, { action: "speedup_minutes", points: 1, per: 1 }]],
  [{ tr: "Kahramanlar", en: "Heroes" }, [{ action: "hero_level", points: 50, per: 1 }, { action: "hero_pull", points: 200, per: 1 }, { action: "hero_recruit", points: 300, per: 1 }]],
  [{ tr: "Topyekûn Seferberlik", en: "Total Mobilisation" }, [{ action: "train_t1", points: 1, per: 1 }, { action: "train_t2", points: 2, per: 1 }, { action: "train_t3", points: 4, per: 1 },
    { action: "train_t4", points: 8, per: 1 }, { action: "train_t5", points: 16, per: 1 }, { action: "gather", points: 1, per: 2000 }, { action: "build_power", points: 1, per: 1 },
    { action: "research", points: 150, per: 1 }, { action: "heal", points: 1, per: 10 }]],
  [{ tr: "Av", en: "The Hunt" }, [{ action: "monster_kill", points: 100, per: 1 }, { action: "monster_levels", points: 20, per: 1 }]],
];
const duelPhases = (hours) => DUEL_DAYS.map(([title, actions], i) => ({ title: Object.assign({}, title), startHour: i * hours, hours, actions: actions.map((a) => Object.assign({}, a)) }));
const DUEL_RESULT = ["sürüyor", "kazandı", "kaybetti", "berabere"];

const EVENT_ACTIONS = {
  gather: "Kaynak toplama (birim)",
  build_power: "İnşaatla kazanılan güç",
  research: "Araştırma seviyesi",
  train_t1: "T1 asker eğitimi", train_t2: "T2 asker eğitimi", train_t3: "T3 asker eğitimi", train_t4: "T4 asker eğitimi", train_t5: "T5 asker eğitimi",
  heal: "Asker iyileştirme",
  monster_kill: "Yaratık yenme",
  monster_levels: "Yenilen yaratık seviyeleri",
  boss_damage: "Etkinlik bossuna verilen hasar (birim)",
  hero_pull: "Kriyo-Kapsül çekimi",
  hero_level: "Kahraman seviye atlatma",
  hero_recruit: "Kahraman işe alma",
  speedup_minutes: "Hızlandırma kullanımı (dakika)",
  hadalite_spent: "Harcanan Hadalit",
  dive: "Derinlik Haritası aşaması",
  help: "İttifak yardımı",
  salvage: "Kurtarma seferi",
  daily_points: "Günlük görev puanı",
};

// Event-only monster kinds (MonsterRules EventOnly): a live event shows them on the map at X:Y until it ends.
const EVENT_BOSSES = { chasm_maw: ["Uçurum Ağzı", 10, 35], depth_kraken: ["Derinlik Kraken'i", 15, 35] };

const EVENT_BANNERS = {
  abyss: ["Uçurum", "#123a6b"], vent: ["Termal Baca", "#c2541b"], bloom: ["Biyolüminesans", "#1aa59a"],
  leviathan: ["Leviathan", "#9c1f2e"], trench: ["Hendek", "#1f6b52"], wreck: ["Batık", "#a07a1c"],
};

const EVENT_PHASES = {
  draft: ["Taslak", ""], upcoming: ["Yaklaşan", ""], live: ["Canlı", "on"], ended: ["Ödül dönemi", ""], closed: ["Bitti", "off"], cancelled: ["İptal", "off"],
};

const EVENT_LANGS = [
  ["tr", "Türkçe"], ["en", "English"], ["de", "Deutsch"], ["fr", "Français"], ["es", "Español"], ["it", "Italiano"], ["pt", "Português"],
  ["nl", "Nederlands"], ["ru", "Русский"], ["ar", "العربية"], ["ja", "日本語"], ["ko", "한국어"], ["zh-Hans", "简体中文"], ["zh-Hant", "繁體中文"],
  ["th", "ไทย"], ["vi", "Tiếng Việt"], ["id", "Bahasa Indonesia"], ["ms", "Bahasa Melayu"],
];

const RARITY_TR = { common: "Sıradan", rare: "Nadir", epic: "Destansı", legendary: "Efsanevi" };

/** Error texts of the event API, added to app.js's table on first use (this file loads before app.js). */
const EVENT_ERRORS = {
  UnknownTemplate: "Etkinlik türü geçersiz.",
  InvalidTitle: "En az bir dilde başlık gir (en fazla 80 karakter).",
  InvalidDescription: "Açıklama en fazla 2000 karakter olabilir.",
  InvalidRange: "Seviye aralığı, ödül süresi (0–14 gün) ya da en düşük puan geçersiz.",
  InvalidBanner: "Bir afiş seç.",
  InvalidDays: "Giriş etkinliğinde 1–31 gün olmalı ve her günün ödülü olmalı.",
  TooManyDays: "Gün sayısı etkinlik süresinden uzun.",
  NoActions: "En az bir puan kazanma yolu ekle.",
  InvalidAction: "Puan kazanma yollarından biri geçersiz (puan > 0, birim ≥ 1).",
  InvalidPhase: "Temalı dönemlerden biri geçersiz (başlık, süre ve en az bir puan yolu gerekir).",
  InvalidStages: "Aşamalar geçersiz: puanlar farklı ve > 0 olmalı, her aşamanın ödülü olmalı.",
  InvalidBands: "Sıralama ödülleri geçersiz: 1'den başlayan sıra aralıkları ve ödüller gerekir.",
  OverlappingBands: "Sıralama aralıkları çakışıyor.",
  InvalidBoss: "Boss geçersiz: yalnız etkinlik bossları, türün seviye aralığında ve harita içinde; en çok 5 boss.",
  InvalidBossRewards: "Boss ödülleri geçersiz: çakışmayan sıra aralıkları (1'den başlar), her aralıkta ödül; yalnız kaynak ve eşya.",
  InvalidReward: "Ödüllerden biri geçersiz.",
  UnknownEvent: "Etkinlik bulunamadı.",
  AlreadyPublished: "Etkinlik zaten yayında.",
  AlreadyOver: "Etkinliğin süresi geçmiş.",
  NotLive: "Etkinlik şu an canlı değil.",
  CancelFirst: "Yayındaki etkinliği silmeden önce iptal et.",
  InvalidKingdoms: "Krallık listesi geçersiz.",
  InvalidSchedule: "Program geçersiz.",
  InvalidId: "Program kimliği 3–32 küçük harf, rakam ya da '-' olmalı.",
  InvalidWeekdays: "En az bir gün seç.",
  InvalidStartTime: "Başlangıç saati geçersiz.",
  InvalidDuration: "Süre 1–168 saat olmalı.",
  InvalidLead: "Önceden oluşturma 0–72 saat olmalı.",
  InvalidBrackets: "Reaktör aralıkları geçersiz (en çok 5; en yüksek ≥ en düşük ya da 0).",
  InvalidBlueprint: "Programın etkinlik şablonu geçersiz.",
  InvalidNote: "Not en fazla 200 karakter olabilir.",
  UnknownSchedule: "Program bulunamadı.",
  NoOccurrence: "Önümüzdeki 8 günde oluşturulacak bir tekrar yok; hepsi zaten var.",
  InvalidMobilization: "Seferberlik ayarları geçersiz (üye 1–200, kart 1–16, yenilenme 0–1440 dk, günlük görev 1–100, yenileme 0–50, hedef %10–1000).",
  InvalidLureTrap: "Yem Kapanı ayarları geçersiz (üye 1–200, savaş 5–240 dk, av başına yem 0–1000, en çok vuran katı 1–10, en çok 10 kademe, eşikler farklı ve pozitif; etkinlik en az 10 dk + savaş süresi kadar sürmeli).",
  InvalidTrench: "Hendek Fethi ayarları geçersiz (reaktör 1–60, kayıtlı üye 1–200, takım başına dövüş 1–5, kayıt 1–336 saat, ilk tur 0–168 saat sonra, tur arası 1–168 saat, galibiyet puanı 0–100000; kayıt + ilk tur bitişten önce bitmeli).",
  InvalidDuelDays: "Düelloda arka arkaya 6 tema günü olmalı (0. saatten başlar, döngü yok).",
  InvalidDuelRewards: "Düello sonuç ödülleri geçersiz (her biri en çok 10 ödül) ya da en az üye sayısı 1–200 değil.",
  AlreadyPaid: "Ödüller postayla gönderildi; süre artık uzatılamaz.",
  AlreadyScoring: "Düelloda puan kazanılmaya başlandı; yeniden eşleştirilemez.",
};

// ---------------------------------------------------------------- Ready-made deep-sea scenarios
const HOUR = 3600 * 1000;

function nextStart() {
  const d = new Date(Date.now() + HOUR);
  d.setMinutes(0, 0, 0);
  return d;
}

const presetRes = (id, amount) => ({ type: "resource", id, amount });
const presetItem = (id, amount) => ({ type: "item", id, amount });

const EVENT_PRESETS = [
  {
    name: "Derinlik Yarışı — 7 gün, 4 saatlik temalar",
    build: (start) => ({
      template: "points", banner: "abyss",
      title: { tr: "Derinlik Yarışı", en: "Depth Race" },
      description: {
        tr: "Uçurumun her katmanı yeni bir sınav. Her 4 saatte tema değişir: kaynak topla, üssünü büyüt, asker eğit, yaratık avla. En derine inen kaşif kazanır.",
        en: "Every layer of the abyss is a new trial. The theme changes every 4 hours: gather, build, train, hunt. The diver who goes deepest wins.",
      },
      startsAt: start, endsAt: new Date(start.getTime() + 7 * 24 * HOUR), claimDays: 3, cyclePhases: true,
      phases: [
        { title: { tr: "Kaynak Akını", en: "Resource Rush" }, startHour: 0, hours: 4, actions: [{ action: "gather", points: 1, per: 1000 }] },
        { title: { tr: "Üs Genişlemesi", en: "Base Expansion" }, startHour: 4, hours: 4, actions: [{ action: "build_power", points: 2, per: 1 }, { action: "speedup_minutes", points: 1, per: 1 }] },
        { title: { tr: "Ordu Kur", en: "Raise the Army" }, startHour: 8, hours: 4, actions: [{ action: "train_t1", points: 1, per: 1 }, { action: "train_t2", points: 2, per: 1 }, { action: "train_t3", points: 4, per: 1 }, { action: "train_t4", points: 8, per: 1 }, { action: "train_t5", points: 16, per: 1 }] },
        { title: { tr: "Yaratık Avı", en: "Creature Hunt" }, startHour: 12, hours: 4, actions: [{ action: "monster_kill", points: 100, per: 1 }, { action: "monster_levels", points: 20, per: 1 }] },
        { title: { tr: "Bilim Dalışı", en: "Science Dive" }, startHour: 16, hours: 4, actions: [{ action: "research", points: 300, per: 1 }, { action: "speedup_minutes", points: 1, per: 1 }] },
        { title: { tr: "Kaşifleri Güçlendir", en: "Empower the Divers" }, startHour: 20, hours: 4, actions: [{ action: "hero_level", points: 50, per: 1 }, { action: "hero_pull", points: 200, per: 1 }] },
      ],
      stages: [
        { points: 2000, rewards: [presetRes("Titanium", 20000), presetItem("speed_any_60", 1)] },
        { points: 8000, rewards: [presetRes("Titanium", 60000), presetItem("chest_silver", 1)] },
        { points: 20000, rewards: [presetRes("Hadalite", 100), presetItem("chest_gold", 1)] },
      ],
      rankBands: [
        { from: 1, to: 1, rewards: [presetRes("Hadalite", 1000), presetItem("chest_gold", 3)] },
        { from: 2, to: 3, rewards: [presetRes("Hadalite", 500), presetItem("chest_gold", 2)] },
        { from: 4, to: 10, rewards: [presetRes("Hadalite", 200), presetItem("chest_silver", 2)] },
        { from: 11, to: 50, rewards: [presetRes("Hadalite", 50), presetItem("chest_bronze", 2)] },
      ],
    }),
  },
  {
    name: "Uçurum Ağzı Uyanıyor — ittifak bossu",
    build: (start) => ({
      template: "alliance", banner: "abyss",
      title: { tr: "Uçurum Ağzı Uyanıyor", en: "The Chasm Maw Wakes" },
      description: {
        tr: "Dev bir goosefish çukur yamacında uyandı. Herkes saldırabilir; her vuruş ortak gövdesinden düşer. İttifakların sırası üyelerinin toplam hasarıyla belirlenir, son darbeyi vuran büyük sandığı alır.",
        en: "A colossal goosefish woke on the trench slope. Anyone can attack; every hit comes off its shared body. Alliances rank by their members' total damage, and the final blow takes the big chest.",
      },
      startsAt: start, endsAt: new Date(start.getTime() + 48 * HOUR), claimDays: 2,
      actions: [{ action: "boss_damage", points: 1, per: 10 }],
      bosses: [{ kind: "chasm_maw", level: 20, x: 100, y: 140 }],
      bossRanks: [
        { from: 1, to: 1, rewards: [presetItem("chest_gold", 2)] },
        { from: 2, to: 2, rewards: [presetItem("chest_gold", 1)] },
        { from: 3, to: 3, rewards: [presetItem("chest_silver", 2)] },
        { from: 4, to: 10, rewards: [presetItem("chest_silver", 1)] },
      ],
      bossParticipation: [presetItem("chest_bronze", 1)],
      stages: [
        { points: 500, rewards: [presetItem("chest_bronze", 1)] },
        { points: 3000, rewards: [presetItem("chest_silver", 1)] },
      ],
      rankBands: [
        { from: 1, to: 1, rewards: [presetRes("Hadalite", 300)] },
        { from: 2, to: 5, rewards: [presetRes("Hadalite", 120)] },
      ],
    }),
  },
  {
    name: "Biyolüminesans Gecesi — hafta sonu yaratık avı",
    build: (start) => ({
      template: "points", banner: "bloom",
      title: { tr: "Biyolüminesans Gecesi", en: "Bioluminescent Night" },
      description: {
        tr: "Derinlik ışıklarla doldu: yaratıklar yüzeye yaklaşıyor. Yenilen her yaratık puan getirir, yüksek seviyeliler daha fazla.",
        en: "The deep is full of light: creatures rise. Every creature defeated scores, higher levels score more.",
      },
      startsAt: start, endsAt: new Date(start.getTime() + 48 * HOUR), claimDays: 2,
      actions: [{ action: "monster_kill", points: 100, per: 1 }, { action: "monster_levels", points: 50, per: 1 }],
      stages: [
        { points: 1000, rewards: [presetItem("speed_heal_60", 2)] },
        { points: 5000, rewards: [presetItem("chest_silver", 1), presetItem("speed_march_p50", 2)] },
        { points: 15000, rewards: [presetItem("chest_gold", 1)] },
      ],
      rankBands: [
        { from: 1, to: 1, rewards: [presetRes("Hadalite", 500)] },
        { from: 2, to: 10, rewards: [presetRes("Hadalite", 150)] },
      ],
    }),
  },
  {
    name: "Termal Baca Seferberliği — ittifak, 3 gün",
    build: (start) => ({
      template: "alliance", banner: "vent",
      title: { tr: "Termal Baca Seferberliği", en: "Hydrothermal Mobilisation" },
      description: {
        tr: "Bacalar uyandı, kaynaklar fışkırıyor. İttifakça topla, eğit, yardımlaş: üyelerin puanı ittifakın puanına eklenir. En güçlü ittifaklar ödüllendirilir.",
        en: "The vents are awake. Gather, train and help as one: members' points add up for the alliance.",
      },
      startsAt: start, endsAt: new Date(start.getTime() + 72 * HOUR), claimDays: 3,
      actions: [{ action: "gather", points: 1, per: 1000 }, { action: "help", points: 20, per: 1 }, { action: "train_t1", points: 1, per: 1 }, { action: "monster_kill", points: 100, per: 1 }],
      stages: [
        { points: 1000, rewards: [presetRes("Titanium", 30000)] },
        { points: 5000, rewards: [presetItem("chest_silver", 1)] },
      ],
      rankBands: [
        { from: 1, to: 1, rewards: [presetRes("Hadalite", 600), presetItem("chest_gold", 1)] },
        { from: 2, to: 3, rewards: [presetRes("Hadalite", 300)] },
        { from: 4, to: 10, rewards: [presetRes("Hadalite", 100)] },
      ],
    }),
  },
  {
    name: "Dalış Günlüğü — 7 günlük giriş ödülü",
    build: (start) => ({
      template: "login", banner: "wreck",
      title: { tr: "Dalış Günlüğü", en: "Dive Log" },
      description: { tr: "Her gün üsse uğra, günlüğe bir satır daha ekle. 7. gün büyük ödül seni bekliyor.", en: "Check in every day. A big reward waits on day 7." },
      startsAt: start, endsAt: new Date(start.getTime() + 7 * 24 * HOUR), claimDays: 0,
      stages: [
        { points: 1, rewards: [presetRes("Titanium", 10000)] }, { points: 2, rewards: [presetItem("speed_any_60", 1)] },
        { points: 3, rewards: [presetRes("Food", 20000)] }, { points: 4, rewards: [presetItem("chest_bronze", 2)] },
        { points: 5, rewards: [presetItem("speed_any_480", 1)] }, { points: 6, rewards: [presetItem("chest_silver", 1)] },
        { points: 7, rewards: [presetRes("Hadalite", 300), presetItem("chest_gold", 1)] },
      ],
    }),
  },
];

// ---------------------------------------------------------------- Helpers
let eventMeta = null;
const loadMeta = async () => (eventMeta = eventMeta || (await api("events/meta")));

const textOf = (map) => (map && (map.tr || map.en || Object.values(map)[0])) || "—";
const phasePill = (phase) => {
  const [label, cls] = EVENT_PHASES[phase] || [phase, ""];
  return el("span", { class: "pill " + cls }, label);
};
const toLocalInput = (iso) => {
  if (!iso) return "";
  const d = new Date(iso);
  const pad = (n) => String(n).padStart(2, "0");
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`;
};

function rewardName(r) {
  if (r.type === "resource") return `${fmtNum(r.amount)} ${RESOURCE_TR[r.id] || r.id}`;
  if (r.type === "shards") return `${fmtNum(r.amount)} kahraman kartı (${r.id ? r.id : RARITY_TR[r.rarity] || r.rarity})`;
  return `${bagName(r.id)} × ${fmtNum(r.amount)}`;
}
const rewardsText = (list) => (list || []).map(rewardName).join(", ") || "—";

// ---------------------------------------------------------------- List and detail
async function events(root, arg) {
  Object.assign(ERRORS, EVENT_ERRORS);
  if (arg === "new") return eventEditor(root, null);
  if (arg && arg.startsWith("edit-")) return eventEditor(root, arg.slice(5));
  if (arg && arg.startsWith("schedule-")) return scheduleEditor(root, arg.slice(9));
  if (arg) return eventDetail(root, arg);

  const [rows, schedules, calendar] = await Promise.all([api("events"), api("schedules"), api("calendar")]);
  render(root,
    el("div", { class: "row spread" }, el("h1", {}, "Etkinlikler"),
      can("gm") ? el("button", { class: "primary", onclick: () => (location.hash = "#/events/new") }, "+ Yeni etkinlik") : null),
    el("p", { class: "muted" }, "Puan yarışmaları, ittifak yarışmaları ve giriş ödülleri. Yayınlanan etkinlik oyunculara anında görünür; başlangıçtan 7 gün önce \"yaklaşan\" olarak listelenir."),
    calendarSection(schedules, calendar),
    el("h2", {}, "Tüm etkinlikler"),
    rows.length === 0 ? el("div", { class: "card muted" }, "Henüz etkinlik yok. Sağ üstten yeni bir etkinlik oluştur ya da hazır şablonlardan birini kullan.") :
    el("table", {},
      el("thead", {}, el("tr", {}, ["", "Etkinlik", "Tür", "Durum", "Zaman", "Katılımcı", "En yüksek", ""].map((h) => el("th", {}, h)))),
      el("tbody", {}, rows.map((r) => {
        const d = r.definition;
        return el("tr", { class: "click", onclick: () => (location.hash = "#/events/" + d.id) },
          el("td", {}, el("span", { class: "swatch", style: `background:${(EVENT_BANNERS[d.banner] || ["", "#333"])[1]}` })),
          el("td", {}, el("b", {}, textOf(d.title)), d.testAccounts && d.testAccounts.length ? el("span", { class: "pill" }, " test") : null),
          el("td", {}, EVENT_TEMPLATES[d.template] || d.template),
          el("td", {}, phasePill(r.phase)),
          el("td", { class: "mono" }, `${fmtDate(d.startsAt)} → ${fmtDate(d.endsAt)}`),
          el("td", {}, fmtNum(r.participants)),
          el("td", {}, fmtNum(r.topPoints)),
          el("td", {}, "›"));
      }))));
}

async function eventDetail(root, id) {
  const data = await api("events/" + id);
  const r = data.event;
  const d = r.definition;
  const team = d.template === "mobilization" || d.template === "alliance_duel";
  const trench = d.template === "trench_conquest";
  const live = team ? await api(`events/${id}/live`) : null;
  const act = (path, body, done) => api(`events/${id}/${path}`, { method: "POST", body }).then(() => { toast(done); route(); }, (e) => toast(e.message, true));
  const hours = el("input", { type: "number", min: "1", value: "24", style: "width:80px" });

  const buttons = can("gm") ? el("div", { class: "row" },
    d.status === "draft" ? el("button", { class: "primary", onclick: () => confirm("Etkinlik yayınlansın mı? Oyuncular hemen görecek.") && act("publish", null, "Yayınlandı.") }, "Yayınla") : null,
    r.phase !== "closed" && r.phase !== "cancelled" ? el("button", { onclick: () => (location.hash = "#/events/edit-" + id) }, "Düzenle") : null,
    el("button", { onclick: () => api(`events/${id}/duplicate`, { method: "POST" }).then((c) => { toast("Kopyalandı (taslak)."); location.hash = "#/events/" + c.id; }, (e) => toast(e.message, true)) }, "Kopyala"),
    el("button", { title: "Bu etkinliği şablon alan, kapalı bir haftalık program oluşturur", onclick: () => api(`schedules/from-event/${encodeURIComponent(id)}`, { method: "POST" })
      .then((r) => { toast("Program oluşturuldu (kapalı). Gün ve saati kontrol edip aç."); location.hash = "#/events/schedule-" + r.schedule.id; }, (e) => toast(e.message, true)) }, "Takvime ekle"),
    r.phase === "live" ? el("button", { onclick: () => confirm("Etkinlik şimdi bitirilsin mi? Sıralama ödülleri ve alınmamış aşama ödülleri postayla gider.") && act("end", null, "Etkinlik bitirildi.") }, "Şimdi bitir") : null,
    (r.phase === "live" || r.phase === "upcoming" || r.phase === "ended") && !d.paidOutAt ? el("span", { class: "inline" }, hours, el("button", { onclick: () => act("extend", { hours: Number(hours.value) }, "Süre uzatıldı.") }, "saat uzat")) : null,
    d.template === "alliance_duel" && r.phase === "live" ? el("button", { title: "Henüz kimse puan kazanmadıysa eşleşmeyi bugünkü güçlere göre yeniden yapar",
      onclick: () => confirm("Düello şimdi yeniden eşleştirilsin mi?") && act("pair", null, "Eşleştirildi.") }, "Şimdi eşleştir") : null,
    d.status === "published" ? el("button", { class: "danger", onclick: () => confirm("Etkinlik iptal edilsin mi? Oyunculardan gizlenir, ödül alınamaz.") && act("cancel", null, "İptal edildi.") }, "İptal et") : null,
    d.status !== "published" ? el("button", { class: "danger", onclick: () => confirm("Etkinlik ve tüm puanları silinsin mi?") &&
      api("events/" + id, { method: "DELETE" }).then(() => { toast("Silindi."); location.hash = "#/events"; }, (e) => toast(e.message, true)) }, "Sil") : null) : null;

  const actionsTable = (list) => el("table", { class: "compact" }, el("tbody", {}, (list || []).map((a) =>
    el("tr", {}, el("td", {}, EVENT_ACTIONS[a.action] || a.action), el("td", { class: "mono" }, `${fmtNum(a.points)} puan / ${fmtNum(a.per)}`)))));

  render(root,
    el("p", {}, el("a", { href: "#/events" }, "‹ Etkinlikler")),
    el("div", { class: "card banner", style: `border-left:6px solid ${(EVENT_BANNERS[d.banner] || ["", "#333"])[1]}` },
      el("div", { class: "row spread" }, el("h1", {}, textOf(d.title), " ", phasePill(r.phase)), el("span", { class: "muted mono" }, d.id)),
      d.scheduleId ? el("p", {}, el("span", { class: "pill" }, "takvim"), " ", el("a", { href: "#/events/schedule-" + d.scheduleId }, "Programı aç: " + d.scheduleId)) : null,
      el("p", { class: "muted" }, `${EVENT_TEMPLATES[d.template]} · ${fmtDate(d.startsAt)} → ${fmtDate(d.endsAt)}` +
        (d.template !== "login" ? ` · ödül dönemi ${d.claimDays} gün` : "") +
        (d.minLevel || d.maxLevel ? ` · Reaktör ${d.minLevel || 1}–${d.maxLevel || "∞"}` : "") +
        (d.testAccounts && d.testAccounts.length ? ` · yalnızca ${d.testAccounts.length} test hesabı` : "")),
      el("p", {}, (d.description && (d.description.tr || d.description.en)) || ""),
      el("div", { class: "tiles" },
        el("div", { class: "tile" }, el("div", { class: "k" }, "Katılımcı"), el("div", { class: "v" }, fmtNum(r.participants))),
        el("div", { class: "tile" }, el("div", { class: "k" }, data.alliance ? "Lider ittifak puanı" : "En yüksek puan"), el("div", { class: "v" }, fmtNum(r.topPoints))),
        el("div", { class: "tile" }, el("div", { class: "k" }, "Bitiş ödemesi (posta)"),
          el("div", { class: "v small" }, d.paidOutAt ? `${fmtDate(d.paidOutAt)} · ${fmtNum(d.paidOutMails)} posta` : r.phase === "ended" ? "gönderiliyor…" : "bitişte"))),
      buttons),
    live ? liveSection(d, live) : null,
    trench ? trenchSection(d) : null,
    d.template === "lure_trap" ? lureSection(d) : null,
    d.template !== "login" && !trench && d.template !== "lure_trap" ? [
      el("h2", {}, "Puan kazanma"),
      d.phases && d.phases.length
        ? el("div", { class: "grid2" }, d.phases.map((p) => el("div", { class: "card" },
            el("b", {}, textOf(p.title)), el("span", { class: "muted" }, ` · ${p.startHour}. saatten ${p.hours} saat` + (d.cyclePhases ? " (döngü)" : "")), actionsTable(p.actions))))
        : actionsTable(d.actions)] : null,
    el("h2", {}, d.template === "login" ? "Günlük ödüller" : d.template === "alliance_duel" ? "Günlük sandıklar (oyuncunun o günkü puanı)" : "Aşama ödülleri"),
    el("table", {}, el("tbody", {}, (d.stages || []).map((s, i) =>
      el("tr", {}, el("td", {}, d.template === "login" ? `${i + 1}. gün` : `${fmtNum(s.points)} puan`), el("td", {}, rewardsText(s.rewards)))))),
    d.template === "alliance_duel" ? [
      el("h2", {}, "Hafta sonucu ödülleri (postayla; ittifak için en az puan kazanan üyelere)"),
      el("table", {}, el("tbody", {},
        el("tr", {}, el("td", {}, "Kazanan"), el("td", {}, rewardsText((d.duel || {}).win))),
        el("tr", {}, el("td", {}, "Kaybeden"), el("td", {}, rewardsText((d.duel || {}).loss))),
        el("tr", {}, el("td", {}, "Berabere"), el("td", {}, rewardsText((d.duel || {}).draw)))))] : null,
    d.template !== "login" && d.template !== "alliance_duel" ? [
      el("h2", {}, "Sıralama ödülleri"),
      el("table", {}, el("tbody", {}, (d.rankBands || []).length === 0 ? el("tr", {}, el("td", { class: "muted" }, "Sıralama ödülü yok.")) :
        d.rankBands.map((b) => el("tr", {}, el("td", {}, b.from === b.to ? `${b.from}.` : `${b.from}–${b.to}.`), el("td", {}, rewardsText(b.rewards)))))),
      el("h2", {}, data.alliance ? "İttifak sıralaması" : "Sıralama"),
      el("table", {},
        el("thead", {}, el("tr", {}, ["Sıra", data.alliance ? "İttifak" : "Oyuncu", "Etiket", "Puan"].map((h) => el("th", {}, h)))),
        el("tbody", {}, data.ranking.length === 0 ? el("tr", {}, el("td", { class: "muted", colspan: "4" }, "Henüz puan yok.")) :
          data.ranking.map((e) => el("tr", {}, el("td", {}, e.rank), el("td", {}, e.name), el("td", { class: "mono" }, e.tag), el("td", { class: "mono" }, fmtNum(e.points))))))] : null);
}

/** Lure Trap: the rules and the alliance tiers (damage over both traps → what every hitter gets). */
function lureSection(d) {
  const l = d.lureTrap || {};
  return el("div", { class: "card" },
    el("h2", {}, "Yem Kapanı — kurallar ve ittifak kademeleri"),
    el("p", { class: "muted" }, `Kapan kurmak için en az ${l.minMembers} üye · Yutucu ${l.fightMinutes} dk kalır · av başına ${l.baitPerKill} yem · ` +
      `her kapanın en çok vuranı kademe ödülünü ${l.topHitterMultiplier} kat alır`),
    el("table", {}, el("tbody", {}, (l.tiers || []).length === 0 ? el("tr", {}, el("td", { class: "muted" }, "Kademe yok.")) :
      l.tiers.map((t, i) => el("tr", {}, el("td", {}, `${i + 1}. kademe · ${fmtNum(t.points)} hasar`), el("td", {}, rewardsText(t.rewards)))))));
}

/** Trench Conquest: the rules and the league as drawn (groups per kingdom, round results). */
function trenchSection(d) {
  const t = d.trench || {};
  const st = d.trenchState || {};
  const groups = st.groups || [];
  const rounds = st.rounds || [];
  const flag = (f) => ["–", "A", "B"][f] || "–";
  const side = (id) => (id === "~bye" ? "(boş)" : id);
  const table = (g) => {
    const pts = {};
    for (const a of g.alliances) pts[a] = 0;
    for (const r of rounds) {
      if (r.a in pts) pts[r.a] += r.aPoints;
      if (r.b in pts) pts[r.b] += r.bPoints;
    }
    return g.alliances.slice().sort((x, y) => pts[y] - pts[x]).map((a) => `${a} ${pts[a]}`).join(" · ");
  };
  return el("div", { class: "card" },
    el("h2", {}, "Hendek Fethi — lig"),
    el("p", { class: "muted" }, `Reaktör ${t.minReactor}+ · ittifak başına en az ${t.minRegistered} kayıtlı takım · takım başına ${t.fightsPerTeam} dövüş · ` +
      `kayıt ${t.registerHours} saat, ilk tur ${t.firstRoundAfterHours} saat sonra, sonra her ${t.roundEveryHours} saatte bir (5 tur) · galibiyet başına ${fmtNum(t.pointsPerWin)} kişisel puan`),
    el("p", { class: "muted" }, st.groupedAt ? `Gruplar çekildi: ${fmtDate(st.groupedAt)} · oynanan tur: ${st.roundsFought || 0}/5` : "Kayıt süresi bitince gruplar çekilir."),
    groups.length === 0 ? null : el("table", {},
      el("thead", {}, el("tr", {}, ["Krallık", "Grup (lig puanı)"].map((h) => el("th", {}, h)))),
      el("tbody", {}, groups.map((g) => el("tr", {}, el("td", { class: "mono" }, g.kingdom), el("td", { class: "mono small" }, table(g)))))),
    rounds.length === 0 ? null : el("table", {},
      el("thead", {}, el("tr", {}, ["Tur", "A", "B", "Hendekler", "Puan", "Galibiyet"].map((h) => el("th", {}, h)))),
      el("tbody", {}, rounds.map((r) => el("tr", {},
        el("td", {}, r.round), el("td", { class: "mono" }, side(r.a)), el("td", { class: "mono" }, side(r.b)),
        el("td", { class: "mono" }, (r.lanes || []).map(flag).join(" ")),
        el("td", { class: "mono" }, `${r.aPoints} – ${r.bPoints}`), el("td", { class: "mono" }, `${r.aWins} – ${r.bWins}`))))));
}

/** Live state of Mobilization (each alliance's task table) and the duel (pairings per kingdom, days, week points). */
function liveSection(d, live) {
  const tier = (c) => (c.kind ? `${MOBILIZATION_KINDS[c.kind] || c.kind} ${fmtNum(c.target)} · ${MOBILIZATION_TIERS[c.tier] || c.tier} · ${fmtNum(c.points)} p`
    : `yenileniyor ${c.readyAtMs ? new Date(c.readyAtMs).toLocaleTimeString() : ""}`);
  if (d.template === "mobilization") {
    const m = d.mobilization || {};
    return el("div", { class: "card" },
      el("h2", {}, "Seferberlik — canlı görev havuzları"),
      el("p", { class: "muted" }, `En az ${m.minMembers} üye · ${m.slots} kart · kart ${m.refillMinutes} dk'da yenilenir · günde ${m.tasksPerDay} görev, ${m.refreshesPerDay} yenileme (${m.refreshCooldownMinutes} dk ara) · hedef %${m.targetPercent}`),
      live.mobilization.length === 0 ? el("p", { class: "muted" }, "Henüz görev havuzu açan ittifak yok.") :
      el("table", {},
        el("thead", {}, el("tr", {}, ["İttifak", "Üye", "Puan", "Görev tutan", "Kartlar"].map((h) => el("th", {}, h)))),
        el("tbody", {}, live.mobilization.map((p) => el("tr", {},
          el("td", {}, `[${p.tag}] ${p.name}`), el("td", {}, fmtNum(p.members)), el("td", { class: "mono" }, fmtNum(p.points)), el("td", {}, fmtNum(p.holding)),
          el("td", { class: "small" }, p.cards.map((c) => el("div", {}, `${c.slot + 1}. ${tier(c)}`))))))));
  }
  const side = (s) => s.allianceId === "~echo" ? "Derinlik Yankısı (bot)" : `[${s.tag}] ${s.name}`;
  const day = (w) => ["·", "A", "B", "="][w] || "·";
  return el("div", { class: "card" },
    el("h2", {}, "İttifak Düellosu — eşleşmeler"),
    el("p", { class: "muted" }, live.pairedAt ? `Eşleştirme: ${fmtDate(live.pairedAt)} · tek kalan ittifak, krallığındaki ittifakların günlük ortalamasıyla oynayan Derinlik Yankısı'yla eşleşir.` : "Etkinlik başlayınca eşleştirilir."),
    live.duel.length === 0 ? el("p", { class: "muted" }, "Eşleşme yok.") :
    el("table", {},
      el("thead", {}, el("tr", {}, ["Krallık", "A", "B", "Günler (A/B/=)", "Hafta puanı", "Gün puanları A", "Gün puanları B"].map((h) => el("th", {}, h)))),
      el("tbody", {}, live.duel.map((p) => el("tr", {},
        el("td", { class: "mono" }, p.kingdom), el("td", {}, side(p.a)), el("td", {}, side(p.b)),
        el("td", { class: "mono" }, p.dayWinners.map(day).join(" ")),
        el("td", { class: "mono" }, `${p.a.wins} – ${p.b.wins}`),
        el("td", { class: "mono small" }, p.a.dayPoints.map(fmtNum).join(" / ")),
        el("td", { class: "mono small" }, p.b.dayPoints.map(fmtNum).join(" / ")))))));
}

// ---------------------------------------------------------------- Editor
async function eventEditor(root, id) {
  const meta = await loadMeta();
  let model;
  if (id) {
    model = (await api("events/" + id)).event.definition;
  } else {
    const start = nextStart();
    model = { template: "points", banner: "abyss", title: {}, description: {}, startsAt: start, endsAt: new Date(start.getTime() + 3 * 24 * HOUR),
      claimDays: 3, minLevel: 0, maxLevel: 0, minRankPoints: 0, testAccounts: [], actions: [{ action: "gather", points: 1, per: 1000 }], phases: [],
      cyclePhases: false, stages: [], rankBands: [], bosses: [], note: "" };
  }
  model.bosses = model.bosses || [];
  model.bossRanks = model.bossRanks || [];
  model.bossParticipation = model.bossParticipation || [];
  model.mobilization = Object.assign({ minMembers: 15, slots: 8, refillMinutes: 5, tasksPerDay: 8, refreshesPerDay: 3, refreshCooldownMinutes: 10, kinds: [], targetPercent: 100 }, model.mobilization || {});
  model.duel = Object.assign({ minMembers: 3, win: [], loss: [], draw: [] }, model.duel || {});
  model.lureTrap = Object.assign({ minMembers: 20, fightMinutes: 30, baitPerKill: 1, topHitterMultiplier: 2, tiers: [] }, model.lureTrap || {});
  model.trench = Object.assign({ minReactor: 10, minRegistered: 15, fightsPerTeam: 2, registerHours: 48, firstRoundAfterHours: 12, roundEveryHours: 24, pointsPerWin: 100 }, model.trench || {});

  const draw = () => render(root, form());

  // A select of reward ids for a reward type.
  function rewardIdSelect(r) {
    const apply = () => { if (r.type === "shards" && s.value.startsWith("rarity:")) { r.id = ""; r.rarity = s.value.slice(7); } else { r.id = s.value; r.rarity = ""; } };
    const s = el("select", { onchange: apply });
    let options = [];
    if (r.type === "resource") options = RESOURCES.map((x) => [x, RESOURCE_TR[x]]);
    else if (r.type === "item") {
      const known = new Set();
      options = GRANT_ITEMS.map(([x, n]) => (known.add(x), [x, n]));
      for (const i of meta.items) if (!known.has(i.id)) options.push([i.id, `${i.id} (${RARITY_TR[i.rarity] || i.rarity})`]);
    } else {
      options = meta.rarities.map((x) => ["rarity:" + x, `Rastgele ${RARITY_TR[x] || x}`]).concat(meta.heroes.map((h) => [h.id, `${h.id} (${RARITY_TR[h.rarity] || h.rarity})`]));
    }
    for (const [value, label] of options) s.append(el("option", { value }, label));
    const current = r.type === "shards" && !r.id ? "rarity:" + (r.rarity || meta.rarities[0]) : r.id;
    s.value = options.some(([v]) => v === current) ? current : options[0][0];
    apply(); // the model always holds what the select shows
    return s;
  }

  function rewardsEditor(list) {
    return el("div", { class: "rewards" },
      list.map((r, i) => el("div", { class: "reward" },
        el("select", { onchange: (e) => { r.type = e.target.value; r.id = ""; r.rarity = ""; r.amount = { resource: 10000, item: 1, shards: 10 }[r.type]; draw(); } },
          ["resource", "item", "shards"].map((t) => el("option", { value: t, selected: r.type === t }, { resource: "Kaynak", item: "Eşya", shards: "Kahraman kartı" }[t]))),
        rewardIdSelect(r),
        el("input", { type: "number", min: "1", value: r.amount, onchange: (e) => (r.amount = Number(e.target.value)) }),
        el("button", { class: "ghost", title: "Kaldır", onclick: () => { list.splice(i, 1); draw(); } }, "×"))),
      el("button", { class: "ghost small", onclick: () => { list.push({ type: "resource", id: "Titanium", amount: 10000 }); draw(); } }, "+ ödül"));
  }

  function actionsEditor(list) {
    return el("div", {},
      el("table", { class: "compact" }, el("tbody", {}, list.map((a, i) => el("tr", {},
        el("td", {}, el("select", { onchange: (e) => (a.action = e.target.value) },
          meta.actions.map((x) => el("option", { value: x, selected: a.action === x }, EVENT_ACTIONS[x] || x)))),
        el("td", {}, el("input", { type: "number", min: "1", value: a.points, onchange: (e) => (a.points = Number(e.target.value)) }), " puan"),
        el("td", {}, "her ", el("input", { type: "number", min: "1", value: a.per, onchange: (e) => (a.per = Number(e.target.value)) }), " birim için"),
        el("td", {}, el("button", { class: "ghost", onclick: () => { list.splice(i, 1); draw(); } }, "×")))))),
      el("button", { class: "ghost small", onclick: () => { list.push({ action: "monster_kill", points: 10, per: 1 }); draw(); } }, "+ puan yolu"));
  }

  function textInputs(map, multiline, placeholder) {
    const field = (code) => multiline
      ? el("textarea", { class: "short", placeholder, onchange: (e) => (map[code] = e.target.value) }, map[code] || "")
      : el("input", { value: map[code] || "", placeholder, onchange: (e) => (map[code] = e.target.value) });
    return el("div", {},
      el("div", { class: "row" }, el("label", {}, "Türkçe", field("tr")), el("label", {}, "English", field("en"))),
      el("details", {}, el("summary", { class: "muted" }, "Diğer diller (boş bırakılırsa İngilizce gösterilir)"),
        el("div", { class: "row" }, EVENT_LANGS.slice(2).map(([code, name]) => el("label", {}, name, field(code))))));
  }

  async function save() {
    const noScoring = model.template === "login" || model.template === "mobilization" || model.template === "trench_conquest" || model.template === "lure_trap";
    const body = Object.assign({}, model, {
      startsAt: new Date(model.startsAt).toISOString(), endsAt: new Date(model.endsAt).toISOString(),
      phases: noScoring ? [] : model.phases, actions: noScoring || model.template === "alliance_duel" ? [] : model.actions,
      rankBands: model.template === "login" || model.template === "alliance_duel" ? [] : model.rankBands,
      cyclePhases: model.template === "alliance_duel" ? false : model.cyclePhases,
    });
    if (model.template === "login") body.stages = model.stages.map((s, i) => Object.assign({}, s, { points: i + 1 }));
    const saved = await api("events", { method: "POST", body });
    toast(id ? "Kaydedildi." : "Taslak oluşturuldu. Kontrol edip yayınla.");
    location.hash = "#/events/" + saved.id;
  }

  function form() {
    const login = model.template === "login";
    const banner = el("select", { onchange: (e) => { model.banner = e.target.value; draw(); } },
      meta.banners.map((b) => el("option", { value: b, selected: model.banner === b }, (EVENT_BANNERS[b] || [b])[0])));
    return [
      el("p", {}, el("a", { href: id ? "#/events/" + id : "#/events" }, "‹ Geri")),
      el("h1", {}, id ? "Etkinliği düzenle" : "Yeni etkinlik"),
      id ? null : el("div", { class: "card" }, el("b", {}, "Hazır derin deniz senaryoları"),
        el("p", { class: "muted" }, "Birini seç, formu doldursun; sonra istediğin gibi değiştir."),
        el("div", { class: "row" }, EVENT_PRESETS.map((p) => el("button", { onclick: () => { model = Object.assign(model, p.build(nextStart())); draw(); } }, p.name)))),

      el("div", { class: "card" },
        el("h2", {}, "Temel"),
        el("div", { class: "row" },
          el("label", {}, "Tür", el("select", { onchange: (e) => {
            model.template = e.target.value;
            // A duel is six back-to-back themed days: fill them in (editable), and the window to match.
            // A league needs registration, the draw and five rounds inside the window.
            if (model.template === "trench_conquest") {
              const t = model.trench;
              model.endsAt = new Date(new Date(model.startsAt).getTime() + (t.registerHours + t.firstRoundAfterHours + 4 * t.roundEveryHours + 2) * HOUR);
            }
            if (model.template === "alliance_duel" && model.phases.length !== 6) {
              model.phases = duelPhases(24);
              model.cyclePhases = false;
              model.endsAt = new Date(new Date(model.startsAt).getTime() + 6 * 24 * HOUR);
            }
            draw(); } },
            Object.entries(EVENT_TEMPLATES).map(([k, v]) => el("option", { value: k, selected: model.template === k }, v)))),
          el("label", {}, "Afiş", banner),
          el("span", { class: "swatch big", style: `background:${(EVENT_BANNERS[model.banner] || ["", "#333"])[1]}` })),
        el("div", { class: "row" },
          el("label", {}, "Başlangıç", el("input", { type: "datetime-local", value: toLocalInput(model.startsAt), onchange: (e) => (model.startsAt = new Date(e.target.value)) })),
          el("label", {}, "Bitiş", el("input", { type: "datetime-local", value: toLocalInput(model.endsAt), onchange: (e) => (model.endsAt = new Date(e.target.value)) })),
          login ? null : el("label", {}, "Ödül dönemi (gün)", el("input", { type: "number", min: "0", max: "14", value: model.claimDays, onchange: (e) => (model.claimDays = Number(e.target.value)) }))),
        el("div", { class: "row" },
          el("label", {}, "En düşük reaktör (0 = yok)", el("input", { type: "number", min: "0", value: model.minLevel, onchange: (e) => (model.minLevel = Number(e.target.value)) })),
          el("label", {}, "En yüksek reaktör (0 = yok)", el("input", { type: "number", min: "0", value: model.maxLevel, onchange: (e) => (model.maxLevel = Number(e.target.value)) })),
          login ? null : el("label", {}, "Sıralama ödülü için en az puan", el("input", { type: "number", min: "0", value: model.minRankPoints, onchange: (e) => (model.minRankPoints = Number(e.target.value)) }))),
        el("label", {}, "Test hesapları (her satıra bir hesap no; boş = herkes)",
          el("textarea", { class: "short", onchange: (e) => (model.testAccounts = e.target.value.split(/\s+/).filter(Boolean)) }, (model.testAccounts || []).join("\n"))),
        el("label", {}, "İç not (oyuncular görmez)", el("input", { value: model.note || "", onchange: (e) => (model.note = e.target.value) }))),

      el("div", { class: "card" }, el("h2", {}, "Başlık"), textInputs(model.title, false, "Etkinlik adı"),
        el("h2", {}, "Açıklama"), textInputs(model.description, true, "Oyuncunun göreceği hikâye ve kurallar")),

      model.template === "mobilization" ? el("div", { class: "card" },
        el("h2", {}, "Seferberlik ayarları"),
        el("p", { class: "muted" }, "Her ittifakın kendi görev kartları olur; üye bir seferde bir görev alır, oynayarak bitirir, puan hem kendisine hem ittifaka yazılır. Kart renkleri: yeşil (×1, 100 p), mavi (×2, 200 p), mor (×3, 340 p), altın (×5, 560 p). Aşama ödülleri kişisel puan eşikleri, sıralama ödülleri ittifak sırasıdır."),
        el("div", { class: "row" },
          ...[["minMembers", "En az üye"], ["slots", "Kart sayısı (1–16)"], ["refillMinutes", "Alınan kartın yenilenmesi (dk)"], ["tasksPerDay", "Günlük görev hakkı"],
            ["refreshesPerDay", "Günlük yenileme hakkı"], ["refreshCooldownMinutes", "Yenileme arası (dk)"], ["targetPercent", "Hedef büyüklüğü (%)"]]
            .map(([k, label]) => el("label", {}, label, el("input", { type: "number", min: "0", value: model.mobilization[k], onchange: (e) => (model.mobilization[k] = Number(e.target.value)) })))),
        el("label", {}, "Görev türleri (hiçbiri seçili değilse hepsi)"),
        el("div", { class: "row" }, Object.entries(MOBILIZATION_KINDS).map(([k, label]) => el("label", { class: "check" },
          el("input", { type: "checkbox", checked: model.mobilization.kinds.includes(k), onchange: (e) => {
            model.mobilization.kinds = e.target.checked ? model.mobilization.kinds.concat(k) : model.mobilization.kinds.filter((x) => x !== k);
          } }), " " + label)))) : null,

      model.template === "lure_trap" ? el("div", { class: "card" },
        el("h2", {}, "Yem Kapanı ayarları"),
        el("p", { class: "muted" }, "Üyelerin avladığı her yaratık ittifak kapanına yem ekler (seviye başına vuruşlara +%5). Lider ya da savaş görevlisi iki kapanın saatini ve zorluğunu seçer; saati gelince ittifakın toplanma noktasında yalnızca o ittifakın gördüğü, karşılık vermeyen bir Yutucu belirir. Hasar kişisel puandır (aşama ödülleri) ve ittifak puanıdır (sıralama ödülleri). Etkinlik bitince Yutucu'yu vuran herkes, ittifakın iki kapandaki toplam hasarıyla ulaştığı kademenin ödülünü alır."),
        el("div", { class: "row" },
          ...[["minMembers", "Kapan için en az üye"], ["fightMinutes", "Yutucu süresi (dk)"], ["baitPerKill", "Av başına yem"], ["topHitterMultiplier", "En çok vuranın katı"]]
            .map(([k, label]) => el("label", {}, label, el("input", { type: "number", min: "0", value: model.lureTrap[k], onchange: (e) => (model.lureTrap[k] = Number(e.target.value)) })))),
        el("p", {}, el("b", {}, "İttifak kademeleri (toplam hasar eşiği → vuran her üyeye)")),
        el("table", {}, el("tbody", {}, model.lureTrap.tiers.map((t, i) => el("tr", {},
          el("td", {}, el("input", { type: "number", min: "1", value: t.points, onchange: (e) => (t.points = Number(e.target.value)) }), " hasar"),
          el("td", {}, rewardsEditor(t.rewards)),
          el("td", {}, el("button", { class: "ghost", onclick: () => { model.lureTrap.tiers.splice(i, 1); draw(); } }, "×")))))),
        el("button", { onclick: () => {
          const last = model.lureTrap.tiers[model.lureTrap.tiers.length - 1];
          model.lureTrap.tiers.push({ points: last ? last.points * 4 : 20000, rewards: [{ type: "item", id: "chest_bronze", amount: 1 }] });
          draw(); } }, "+ kademe")) : null,

      model.template === "trench_conquest" ? el("div", { class: "card" },
        el("h2", {}, "Hendek Fethi ayarları"),
        el("p", { class: "muted" }, "Üyeler kayıt süresinde 3 hendekten birine bir takım kaydeder (takım o anki hâliyle kopyalanır, askerler ölmez). Kayıt bitince ittifaklar güce göre altışarlı gruplara ayrılır; 5 lig turunda her hendekte takımlar güç sırasıyla bayrak yarışı gibi dövüşür. Daha çok hendek alan kazanır (3 puan), beraberlik 1 puan. Aşama ödülleri üyenin galibiyet puanıyla açılır, sıralama ödülleri ittifakın lig sırasıdır. Bitiş, kayıt + ilk tur + 4 tur arasından sonra olmalı."),
        el("div", { class: "row" },
          ...[["minReactor", "En düşük reaktör"], ["minRegistered", "İttifak için en az kayıtlı takım"], ["fightsPerTeam", "Takım başına dövüş (tur)"],
            ["registerHours", "Kayıt süresi (saat)"], ["firstRoundAfterHours", "İlk tur, kayıttan sonra (saat)"], ["roundEveryHours", "Tur arası (saat)"], ["pointsPerWin", "Galibiyet başına kişisel puan"]]
            .map(([k, label]) => el("label", {}, label, el("input", { type: "number", min: "0", value: model.trench[k], onchange: (e) => (model.trench[k] = Number(e.target.value)) })))),
        el("button", { onclick: () => {
          const t = model.trench;
          model.endsAt = new Date(new Date(model.startsAt).getTime() + (t.registerHours + t.firstRoundAfterHours + 4 * t.roundEveryHours + 2) * HOUR);
          draw(); } }, "Bitişi 5 tura göre ayarla")) : null,

      model.template === "alliance_duel" ? el("div", { class: "card" },
        el("h2", {}, "Düello ayarları"),
        el("p", { class: "muted" }, "Etkinlik canlı olunca her krallığın ittifakları güce göre ikişer eşleşir; tek kalan, krallığın günlük ortalamasıyla oynayan Derinlik Yankısı'yla eşleşir. 6 gün = 6 tema (aşağıda); gün kazanmak 1/2/2/2/2/4 puan, 7'ye ulaşan kazanır. Aşama ödülleri her günün kişisel puanıyla açılan günlük sandıklardır."),
        el("div", { class: "row" },
          el("label", {}, "Eşleşmek için en az üye", el("input", { type: "number", min: "1", value: model.duel.minMembers, onchange: (e) => (model.duel.minMembers = Number(e.target.value)) })),
          el("button", { onclick: () => { const h = model.phases.length ? model.phases[0].hours : 24; model.phases = duelPhases(h); draw(); } }, "6 temayı varsayılana döndür")),
        el("p", {}, el("b", {}, "Kazanan ittifakın üyelerine")), rewardsEditor(model.duel.win),
        el("p", {}, el("b", {}, "Kaybeden ittifakın üyelerine")), rewardsEditor(model.duel.loss),
        el("p", {}, el("b", {}, "Beraberlikte iki tarafa")), rewardsEditor(model.duel.draw),
        el("p", { class: "muted" }, "Sonuç postası, ittifakı için en az \"sıralama ödülü için en az puan\" kadar kazanan her üyeye gider.")) : null,

      login || model.template === "mobilization" || model.template === "trench_conquest" || model.template === "lure_trap" ? null : el("div", { class: "card" },
        el("h2", {}, "Puan kazanma yolları"),
        el("label", { class: "check" }, el("input", { type: "checkbox", checked: model.phases.length > 0, onchange: (e) => {
          model.phases = e.target.checked ? [{ title: { tr: "1. tema", en: "Theme 1" }, startHour: 0, hours: 4, actions: model.actions.length ? model.actions : [{ action: "gather", points: 1, per: 1000 }] }] : [];
          draw(); } }), " Temalı dönemler (her dönem kendi puan yollarıyla)"),
        model.phases.length === 0 ? actionsEditor(model.actions) : [
          el("label", { class: "check" }, el("input", { type: "checkbox", checked: model.cyclePhases, onchange: (e) => (model.cyclePhases = e.target.checked) }), " Dönemler etkinlik bitene kadar döngüyle tekrar etsin"),
          model.phases.map((p, i) => el("div", { class: "card inner" },
            el("div", { class: "row" },
              el("label", {}, "Dönem adı (TR)", el("input", { value: p.title.tr || "", onchange: (e) => (p.title.tr = e.target.value) })),
              el("label", {}, "Name (EN)", el("input", { value: p.title.en || "", onchange: (e) => (p.title.en = e.target.value) })),
              el("label", {}, "Başlangıç (saat)", el("input", { type: "number", min: "0", step: "0.5", value: p.startHour, onchange: (e) => (p.startHour = Number(e.target.value)) })),
              el("label", {}, "Süre (saat)", el("input", { type: "number", min: "0.5", step: "0.5", value: p.hours, onchange: (e) => (p.hours = Number(e.target.value)) })),
              el("button", { class: "danger", onclick: () => { model.phases.splice(i, 1); draw(); } }, "Dönemi sil")),
            actionsEditor(p.actions))),
          el("button", { onclick: () => {
            const last = model.phases[model.phases.length - 1];
            model.phases.push({ title: { tr: `${model.phases.length + 1}. tema`, en: `Theme ${model.phases.length + 1}` }, startHour: last ? last.startHour + last.hours : 0, hours: last ? last.hours : 4, actions: [{ action: "gather", points: 1, per: 1000 }] });
            draw(); } }, "+ dönem")]),

      el("div", { class: "card" },
        el("h2", {}, login ? "Günlük ödüller" : model.template === "alliance_duel" ? "Günlük sandıklar (oyuncunun o günkü puanı)" : "Aşama ödülleri (puan eşikleri)"),
        el("table", {}, el("tbody", {}, model.stages.map((s, i) => el("tr", {},
          el("td", {}, login ? `${i + 1}. gün` : el("span", {}, el("input", { type: "number", min: "1", value: s.points, onchange: (e) => (s.points = Number(e.target.value)) }), " puan")),
          el("td", {}, rewardsEditor(s.rewards)),
          el("td", {}, el("button", { class: "ghost", onclick: () => { model.stages.splice(i, 1); draw(); } }, "×")))))),
        el("button", { onclick: () => {
          const last = model.stages[model.stages.length - 1];
          model.stages.push({ points: login ? model.stages.length + 1 : (last ? last.points * 2 : 1000), rewards: [{ type: "resource", id: "Titanium", amount: 10000 }] });
          draw(); } }, login ? "+ gün" : "+ aşama")),

      login || model.template === "alliance_duel" ? null : el("div", { class: "card" },
        el("h2", {}, model.template === "alliance" || model.template === "mobilization" || model.template === "trench_conquest" || model.template === "lure_trap" ? "Sıralama ödülleri (ittifak sırası; ödülü puan kazanan her üye postayla alır)" : "Sıralama ödülleri (bitişte postayla)"),
        el("table", {}, el("tbody", {}, model.rankBands.map((b, i) => el("tr", {},
          el("td", {}, el("input", { type: "number", min: "1", value: b.from, onchange: (e) => (b.from = Number(e.target.value)) }), " – ",
            el("input", { type: "number", min: "1", value: b.to, onchange: (e) => (b.to = Number(e.target.value)) }), ". sıra"),
          el("td", {}, rewardsEditor(b.rewards)),
          el("td", {}, el("button", { class: "ghost", onclick: () => { model.rankBands.splice(i, 1); draw(); } }, "×")))))),
        el("button", { onclick: () => {
          const last = model.rankBands[model.rankBands.length - 1];
          const from = last ? last.to + 1 : 1;
          model.rankBands.push({ from, to: from, rewards: [{ type: "resource", id: "Hadalite", amount: 100 }] });
          draw(); } }, "+ sıra aralığı")),

      el("div", { class: "card" },
        el("h2", {}, "Etkinlik bossları"),
        el("p", { class: "muted" }, "Etkinlik canlıyken haritada X:Y'de durur (doluysa en yakın boş kareye); bitince kaybolur. Herkes saldırabilir, hasar ortak gövdeden düşer. Puan için 'Etkinlik bossuna verilen hasar' yolunu ekle."),
        el("table", { class: "compact" }, el("tbody", {}, model.bosses.map((b, i) => el("tr", {},
          el("td", {}, el("select", { onchange: (e) => (b.kind = e.target.value) },
            Object.entries(EVENT_BOSSES).map(([k, v]) => el("option", { value: k, selected: b.kind === k }, `${v[0]} (sv ${v[1]}–${v[2]})`)))),
          el("td", {}, "Sv ", el("input", { type: "number", min: "1", max: "35", value: b.level, onchange: (e) => (b.level = Number(e.target.value)) })),
          el("td", {}, "X ", el("input", { type: "number", min: "0", value: b.x, onchange: (e) => (b.x = Number(e.target.value)) })),
          el("td", {}, "Y ", el("input", { type: "number", min: "0", value: b.y, onchange: (e) => (b.y = Number(e.target.value)) })),
          el("td", {}, el("button", { class: "ghost", onclick: () => { model.bosses.splice(i, 1); draw(); } }, "×")))))),
        model.bosses.length >= 5 ? null : el("button", { class: "ghost small", onclick: () => { model.bosses.push({ kind: "chasm_maw", level: 20, x: 100, y: 140 }); draw(); } }, "+ boss"),
        model.bosses.length === 0 ? null : [
          el("h2", {}, "Boss ödülleri (ittifak içi hasar sırası)"),
          el("p", { class: "muted" }, "Boss ölünce ya da etkinlik bitince, ona vuran herkes posta ile ödül alır. Her üye kendi ittifakı içinde verdiği hasara göre sıralanır; sıra aralığına düşmeyenler ve ittifaksızlar katılım ödülünü alır. Hepsi boşsa varsayılan: 1. altın+gümüş sandık, 2. altın, 3. iki gümüş, 4–10. gümüş, diğerleri bronz. Yalnız kaynak ve eşya (kart parçası postayla gitmez)."),
          el("table", {}, el("tbody", {}, model.bossRanks.map((b, i) => el("tr", {},
            el("td", {}, el("input", { type: "number", min: "1", value: b.from, onchange: (e) => (b.from = Number(e.target.value)) }), " – ",
              el("input", { type: "number", min: "1", value: b.to, onchange: (e) => (b.to = Number(e.target.value)) }), ". sıra"),
            el("td", {}, rewardsEditor(b.rewards)),
            el("td", {}, el("button", { class: "ghost", onclick: () => { model.bossRanks.splice(i, 1); draw(); } }, "×")))))),
          model.bossRanks.length >= 10 ? null : el("button", { class: "ghost small", onclick: () => {
            const last = model.bossRanks[model.bossRanks.length - 1];
            const from = last ? last.to + 1 : 1;
            model.bossRanks.push({ from, to: from, rewards: [presetItem(from === 1 ? "chest_gold" : "chest_silver", 1)] });
            draw(); } }, "+ sıra aralığı"),
          el("p", {}, el("b", {}, "Katılım ödülü (geri kalan herkes)")),
          rewardsEditor(model.bossParticipation)]),

      el("div", { class: "row" },
        el("button", { class: "primary", onclick: () => save().catch((e) => toast(e.message, true)) }, id ? "Kaydet" : "Taslak olarak kaydet"),
        el("span", { class: "muted" }, id ? "Canlı bir etkinlikte tür ve başlangıç değişmez; kazanılmış puanlar korunur." : "Taslaklar oyunculara görünmez; ayrıntı sayfasından yayınlarsın.")),
    ];
  }

  draw();
}

// ---------------------------------------------------------------- Weekly calendar (docs/29 §2.2, roadmap §7)
const WEEKDAYS_TR = ["Paz", "Pzt", "Sal", "Çar", "Per", "Cum", "Cmt"]; // System.DayOfWeek order
const CAL_STATES = {
  planned: ["Planlı", ""], disabled: ["Kapalı program", "off"], skipped: ["Silinmiş", "off"], draft: ["Taslak", ""], cancelled: ["İptal", "off"],
  upcoming: ["Yaklaşan", ""], live: ["Canlı", "on"], ended: ["Ödül dönemi", ""], closed: ["Bitti", "off"],
};
const pad2 = (n) => String(n).padStart(2, "0");
const utcTime = (iso) => { const d = new Date(iso); return `${pad2(d.getUTCHours())}:${pad2(d.getUTCMinutes())}`; };
const utcDay = (iso) => { const d = new Date(iso); return `${WEEKDAYS_TR[d.getUTCDay()]} ${pad2(d.getUTCDate())}.${pad2(d.getUTCMonth() + 1)}`; };
const minuteText = (m) => `${pad2(Math.floor(m / 60))}:${pad2(m % 60)}`;
const calPill = (state) => { const [label, cls] = CAL_STATES[state] || [state, ""]; return el("span", { class: "pill " + cls }, label); };
const daysText = (days) => (days.length === 7 ? "Her gün" : days.slice().sort((a, b) => ((a + 6) % 7) - ((b + 6) % 7)).map((d) => WEEKDAYS_TR[d]).join(", "));

function calendarSection(schedules, calendar) {
  const runNow = (id) => api(`schedules/${id}/run`, { method: "POST" })
    .then((r) => { toast(`Oluşturuldu: ${r.created.join(", ")}`); route(); }, (e) => toast(e.message, true));
  const setEnabled = (id, enabled) => api(`schedules/${id}/enabled`, { method: "POST", body: { enabled } })
    .then(() => { toast(enabled ? "Program açıldı." : "Program kapatıldı."); route(); }, (e) => toast(e.message, true));

  // The week grid: a column per UTC day, a row per kingdom; an occurrence shows on every day it covers.
  const kingdoms = calendar.kingdoms.length ? calendar.kingdoms : [{ id: "w1", number: 1, name: "" }];
  const dayStart = (iso) => new Date(iso).getTime();
  const DAY = 24 * HOUR;
  const cell = (kingdom, day) => {
    const from = dayStart(day), to = from + DAY;
    const items = calendar.entries.filter((e) => (e.kingdom === kingdom || e.kingdom === "") && dayStart(e.startsAt) < to && dayStart(e.endsAt) > from);
    return el("td", { class: "cal-cell" }, items.length === 0 ? el("span", { class: "muted" }, "—") : items.map((e) => {
      const starts = dayStart(e.startsAt) >= from;
      const link = e.state === "planned" || e.state === "disabled" || e.state === "skipped" ? "#/events/schedule-" + e.scheduleId : "#/events/" + e.id;
      return el("a", { class: "cal-item", href: link, style: `border-left-color:${(EVENT_BANNERS[e.banner] || ["", "#333"])[1]}`,
        title: `${e.id}\n${utcDay(e.startsAt)} ${utcTime(e.startsAt)} → ${utcDay(e.endsAt)} ${utcTime(e.endsAt)} UTC` + (e.minLevel || e.maxLevel ? `\nReaktör ${e.minLevel || 1}–${e.maxLevel || "∞"}` : "") },
        el("b", {}, textOf(e.title)), " ", calPill(e.state),
        el("div", { class: "muted small" }, starts ? `${utcTime(e.startsAt)} UTC · ${EVENT_TEMPLATES[e.template] || e.template}` : "← devam ediyor"));
    }));
  };

  return el("div", { class: "card" },
    el("div", { class: "row spread" }, el("h2", {}, "Takvim"), el("span", { class: "muted" }, "Saatler sunucu saati (UTC). Programlar her krallık için etkinliği başlangıçtan önce kendisi oluşturur.")),
    el("div", { class: "cal-scroll" }, el("table", { class: "cal" },
      el("thead", {}, el("tr", {}, el("th", {}, "Krallık"), calendar.days.map((d, i) => el("th", { class: i === 0 ? "today" : "" }, i === 0 ? "Bugün · " + utcDay(d) : utcDay(d))))),
      el("tbody", {}, kingdoms.map((k) => el("tr", {}, el("td", { class: "mono" }, `#${k.number} ${k.name || ""}`.trim()), calendar.days.map((d) => cell(k.id, d))))))),
    el("h2", {}, "Haftalık programlar"),
    schedules.length === 0 ? el("p", { class: "muted" }, "Program yok. Bir etkinliğin sayfasında \"Takvime ekle\" ile oluştur.") :
    el("table", {},
      el("thead", {}, el("tr", {}, ["Program", "Tür", "Günler", "Başlangıç (UTC)", "Süre", "Önceden", "Sonraki", "Durum", ""].map((h) => el("th", {}, h)))),
      el("tbody", {}, schedules.map(({ schedule: s, nextStart }) => el("tr", {},
        el("td", {}, el("a", { href: "#/events/schedule-" + s.id }, el("b", {}, textOf(s.blueprint.title))), el("div", { class: "muted mono" }, s.id)),
        el("td", {}, EVENT_TEMPLATES[s.blueprint.template] || s.blueprint.template),
        el("td", {}, daysText(s.weekdays)),
        el("td", { class: "mono" }, minuteText(s.startMinute)),
        el("td", {}, `${fmtNum(s.durationHours)} sa`),
        el("td", {}, `${fmtNum(s.leadHours)} sa`),
        el("td", { class: "mono" }, nextStart ? `${utcDay(nextStart)} ${utcTime(nextStart)}` : "—"),
        el("td", {}, el("span", { class: "pill " + (s.enabled ? "on" : "off") }, s.enabled ? "Açık" : "Kapalı")),
        el("td", {}, can("gm") ? el("span", { class: "inline" },
          el("button", { class: "small", onclick: () => setEnabled(s.id, !s.enabled) }, s.enabled ? "Kapat" : "Aç"),
          el("button", { class: "small", onclick: () => (location.hash = "#/events/schedule-" + s.id) }, "Düzenle"),
          el("button", { class: "small", title: "Sıradaki tekrarı (eksik krallıklar için) beklemeden şimdi oluşturur", onclick: () => confirm("Sıradaki tekrar şimdi oluşturulsun mu? Oyuncular \"yaklaşan\" olarak görür.") && runNow(s.id) }, "Şimdi oluştur")) : null))))));
}

async function scheduleEditor(root, id) {
  const { schedule: model, nextStart } = await api("schedules/" + id);
  model.brackets = model.brackets || [];
  const b = model.blueprint;
  const draw = () => render(root, form());

  async function save() {
    const saved = await api("schedules", { method: "POST", body: model });
    toast("Program kaydedildi. Yeni ayarlar bundan sonra oluşturulan tekrarlara uygulanır.");
    location.hash = "#/events/schedule-" + saved.schedule.id;
    route();
  }

  function form() {
    const time = el("input", { type: "time", value: minuteText(model.startMinute), onchange: (e) => {
      const [h, m] = e.target.value.split(":").map(Number);
      model.startMinute = (h || 0) * 60 + (m || 0);
    } });
    return [
      el("p", {}, el("a", { href: "#/events" }, "‹ Etkinlikler")),
      el("div", { class: "card banner", style: `border-left:6px solid ${(EVENT_BANNERS[b.banner] || ["", "#333"])[1]}` },
        el("div", { class: "row spread" }, el("h1", {}, textOf(b.title), " ", el("span", { class: "pill " + (model.enabled ? "on" : "off") }, model.enabled ? "Açık" : "Kapalı")),
          el("span", { class: "muted mono" }, model.id)),
        el("p", { class: "muted" }, `${EVENT_TEMPLATES[b.template] || b.template} · ${daysText(model.weekdays)} ${minuteText(model.startMinute)} UTC · ${fmtNum(model.durationHours)} saat` +
          (nextStart ? ` · sonraki başlangıç ${utcDay(nextStart)} ${utcTime(nextStart)} UTC` : "")),
        el("p", {}, (b.description && (b.description.tr || b.description.en)) || ""),
        el("p", { class: "muted" }, `Şablon: ${(b.phases || []).length} tema, ${(b.actions || []).length} puan yolu, ${(b.stages || []).length} ${b.template === "login" ? "gün" : "aşama"}, ${(b.rankBands || []).length} sıralama aralığı` +
          ` · ödül dönemi ${b.claimDays} gün. Şablonu değiştirmek için bir etkinliği düzenle ve onun sayfasından "Takvime ekle" de.`)),

      el("div", { class: "card" },
        el("h2", {}, "Zaman"),
        el("label", {}, "Günler"),
        el("div", { class: "row" }, [1, 2, 3, 4, 5, 6, 0].map((d) => el("label", { class: "check" },
          el("input", { type: "checkbox", checked: model.weekdays.includes(d), onchange: (e) => {
            model.weekdays = e.target.checked ? model.weekdays.concat(d) : model.weekdays.filter((x) => x !== d);
          } }), " " + WEEKDAYS_TR[d]))),
        el("div", { class: "row" },
          el("label", {}, "Başlangıç saati (UTC)", time),
          el("label", {}, "Süre (saat, 1–168)", el("input", { type: "number", min: "1", max: "168", step: "0.5", value: model.durationHours, onchange: (e) => (model.durationHours = Number(e.target.value)) })),
          el("label", {}, "Kaç saat önce oluşturulsun (0–72)", el("input", { type: "number", min: "0", max: "72", value: model.leadHours, onchange: (e) => (model.leadHours = Number(e.target.value)) }))),
        (b.phases || []).length > 1 ? el("label", { class: "check" }, el("input", { type: "checkbox", checked: model.rotatePhases, onchange: (e) => (model.rotatePhases = e.target.checked) }),
          " Temalar her gün bir sıra kaysın (aynı saatte oynayan her temayı görür)") : null,
        el("label", { class: "check" }, el("input", { type: "checkbox", checked: model.enabled, onchange: (e) => (model.enabled = e.target.checked) }), " Program açık")),

      el("div", { class: "card" },
        el("h2", {}, "Kitle"),
        el("label", {}, "Krallıklar (virgülle; boş = hepsi)", el("input", { value: (b.kingdoms || []).join(", "),
          onchange: (e) => (b.kingdoms = e.target.value.split(/[,\s]+/).filter(Boolean)) })),
        el("p", { class: "muted" }, "Reaktör aralıkları: her aralık ve krallık için ayrı etkinlik ve ayrı sıralama. Boşsa tek etkinlik" +
          (b.minLevel || b.maxLevel ? ` (Reaktör ${b.minLevel || 1}–${b.maxLevel || "∞"})` : "") + "."),
        el("table", { class: "compact" }, el("tbody", {}, model.brackets.map((r, i) => el("tr", {},
          el("td", {}, "Reaktör ", el("input", { type: "number", min: "0", value: r.minLevel, onchange: (e) => (r.minLevel = Number(e.target.value)) }), " – ",
            el("input", { type: "number", min: "0", value: r.maxLevel, onchange: (e) => (r.maxLevel = Number(e.target.value)) }), " (0 = sınırsız)"),
          el("td", {}, el("button", { class: "ghost", onclick: () => { model.brackets.splice(i, 1); draw(); } }, "×")))))),
        model.brackets.length >= 5 ? null : el("button", { class: "ghost small", onclick: () => {
          const last = model.brackets[model.brackets.length - 1];
          const from = last && last.maxLevel ? last.maxLevel + 1 : 1;
          model.brackets.push({ minLevel: from, maxLevel: 0 });
          draw(); } }, "+ reaktör aralığı"),
        el("label", {}, "İç not", el("input", { value: model.note || "", onchange: (e) => (model.note = e.target.value) }))),

      can("gm") ? el("div", { class: "row" },
        el("button", { class: "primary", onclick: () => save().catch((e) => toast(e.message, true)) }, "Kaydet"),
        el("button", { onclick: () => confirm("Sıradaki tekrar şimdi oluşturulsun mu?") && api(`schedules/${model.id}/run`, { method: "POST" })
          .then((r) => toast(`Oluşturuldu: ${r.created.join(", ")}`), (e) => toast(e.message, true)) }, "Sıradakini şimdi oluştur"),
        el("button", { class: "danger", onclick: () => confirm("Program silinsin mi? Oluşturduğu etkinlikler kalır.") &&
          api("schedules/" + model.id, { method: "DELETE" }).then(() => { toast("Silindi."); location.hash = "#/events"; }, (e) => toast(e.message, true)) }, "Sil"),
        el("span", { class: "muted" }, "Değişiklikler önceden oluşturulmuş tekrarları değiştirmez.")) : null,
    ];
  }

  draw();
}
