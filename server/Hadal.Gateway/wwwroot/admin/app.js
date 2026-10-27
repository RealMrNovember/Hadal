// HADAL Overseer panel (docs/22). Plain JS, no third-party code: the page runs under a strict CSP.
// The session token lives in sessionStorage and goes out as a Bearer header (no cookies, no CSRF).
"use strict";

const RESOURCES = ["Oxygen", "Energy", "Food", "Biomass", "Titanium", "Hadalite"];
const RESOURCE_TR = { Oxygen: "Oksijen", Energy: "Enerji", Food: "Gıda", Biomass: "Biyokütle", Titanium: "Titanyum", Hadalite: "Hadalit" };
const ROLE_TR = { owner: "Sahip", gm: "Oyun Yöneticisi", viewer: "İzleyici" };
const RANK = { viewer: 1, gm: 2, owner: 3 };

const state = { token: sessionStorage.getItem("overseer.token"), user: null };
const $ = (sel, root = document) => root.querySelector(sel);

// ---------------------------------------------------------------- DOM helpers (no innerHTML with data)
function el(tag, attrs = {}, ...children) {
  const node = document.createElement(tag);
  for (const [key, value] of Object.entries(attrs)) {
    if (value === undefined || value === null || value === false) continue;
    if (key === "class") node.className = value;
    else if (key.startsWith("on")) node.addEventListener(key.slice(2), value);
    else if (key === "value") node.value = value;
    else node.setAttribute(key, value === true ? "" : value);
  }
  for (const child of children.flat()) {
    if (child === null || child === undefined || child === false) continue;
    node.append(child instanceof Node ? child : document.createTextNode(String(child)));
  }
  return node;
}

/** replaceChildren that skips the null/false placeholders of conditional sections. */
const render = (root, ...children) => root.replaceChildren(...children.flat().filter((c) => c !== null && c !== undefined && c !== false));

function toast(text, bad = false) {
  const t = $("#toast");
  t.textContent = text;
  t.className = "toast show" + (bad ? " bad" : "");
  clearTimeout(toast.timer);
  toast.timer = setTimeout(() => (t.className = "toast"), 3200);
}

const fmtDate = (iso) => (iso ? new Date(iso).toLocaleString("tr-TR") : "—");
const fmtNum = (n) => Number(n || 0).toLocaleString("tr-TR");
const can = (role) => state.user && RANK[state.user.role] >= RANK[role];

// ---------------------------------------------------------------- API
async function api(path, options = {}) {
  const response = await fetch("api/" + path, {
    method: options.method || "GET",
    headers: Object.assign({ "Content-Type": "application/json" }, state.token ? { Authorization: "Bearer " + state.token } : {}),
    body: options.body ? JSON.stringify(options.body) : undefined,
  });
  if (response.status === 401 && path !== "login") {
    logout();
    throw new Error("Oturum sona erdi.");
  }
  const text = await response.text();
  const data = text ? JSON.parse(text) : null;
  if (!response.ok) throw new Error(explain(data && data.error) + (data && data.detail ? "\n" + data.detail : ""));
  return data;
}

const ERRORS = {
  InvalidCredentials: "Kullanıcı adı ya da şifre hatalı.",
  Forbidden: "Bu işlem için yetkin yok.",
  InvalidAmounts: "Geçerli bir miktar gir.",
  InvalidRewards: "En az bir pozitif ödül gir.",
  InvalidCode: "Kod 4–24 harf/rakam olmalı.",
  CodeExists: "Bu kod zaten var.",
  InvalidWindow: "Başlangıç, bitişten önce olmalı.",
  UnknownPlayer: "Oyuncu bulunamadı.",
  PlayerNotStarted: "Oyuncu henüz oyuna girmemiş.",
  InvalidCatalog: "Katalog geçersiz:",
  InvalidHours: "Süre (saat) gir.",
  InvalidAdmin: "Kullanıcı adı 3–32 karakter; rol sahip/gm/izleyici.",
  UsernameExists: "Bu kullanıcı adı alınmış.",
  CannotDisableSelf: "Kendi hesabını kapatamazsın.",
  InvalidVersion: "Sürüm 1.2.0 biçiminde olmalı.",
  InvalidUrl: "İndirme adresi https:// ile başlamalı.",
  ConfirmationRequired: "Silme onaylanmadı.",
};
const explain = (code) => ERRORS[code] || code || "İşlem başarısız.";

// ---------------------------------------------------------------- Session
async function login(event) {
  event.preventDefault();
  $("#login-error").textContent = "";
  try {
    const data = await api("login", {
      method: "POST",
      body: { username: $("#login-user").value.trim(), password: $("#login-pass").value },
    });
    state.token = data.token;
    sessionStorage.setItem("overseer.token", data.token);
    $("#login-pass").value = "";
    await start();
  } catch (error) {
    $("#login-error").textContent = error.message;
  }
}

function logout() {
  state.token = null;
  state.user = null;
  sessionStorage.removeItem("overseer.token");
  $("#app").classList.add("hidden");
  $("#login").classList.remove("hidden");
}

async function start() {
  if (!state.token) return logout();
  try {
    state.user = await api("me");
  } catch {
    return logout();
  }
  $("#who").textContent = `${state.user.username} · ${ROLE_TR[state.user.role] || state.user.role}`;
  document.querySelectorAll(".owner-only").forEach((n) => n.classList.toggle("hidden", !can("owner")));
  $("#login").classList.add("hidden");
  $("#app").classList.remove("hidden");
  route();
}

// ---------------------------------------------------------------- Router
const PAGES = { overview, players, coupons, liveops, balance, chat, audit, admins };

function route() {
  if (!state.user) return;
  const [name, arg] = (location.hash.replace(/^#\//, "") || "overview").split("/");
  const page = PAGES[name] ? name : "overview";
  document.querySelectorAll(".sidebar a").forEach((a) => a.classList.toggle("active", a.dataset.page === page));
  const root = $("#page");
  render(root, el("p", { class: "muted" }, "Yükleniyor…"));
  PAGES[page](root, arg).catch((error) => {
    render(root, el("p", { class: "error" }, error.message));
  });
}

// ---------------------------------------------------------------- Pages
async function overview(root) {
  const s = await api("stats");
  const tile = (k, v, cls = "") => el("div", { class: "tile" }, el("div", { class: "k" }, k), el("div", { class: "v " + cls }, v));
  render(root, 
    el("h1", {}, "Özet"),
    el("div", { class: "tiles" },
      tile("Toplam oyuncu", fmtNum(s.players)),
      tile("Son 24 saat", fmtNum(s.playersLast24h)),
      tile("Şu an çevrimiçi", fmtNum(s.online), "ok"),
      tile("Kupon kullanımı", fmtNum(s.couponRedemptions)),
      tile("Katalog sürümü", "v" + s.catalogVersion),
      tile("Bakım modu", s.liveOps.maintenance ? "AÇIK" : "Kapalı", s.liveOps.maintenance ? "bad" : "ok")),
    s.liveOps.announcement ? el("p", { class: "muted" }, "Duyuru: " + s.liveOps.announcement) : null);
}

// A test leftover: no name, or made by a smoke/load run rather than the game client.
const looksTest = (p) => !p.displayName || p.clientVersion === "smoke" || p.clientVersion === "";

async function deleteAccounts(ids) {
  let done = 0;
  for (const id of ids) {
    await api(`players/${id}/delete`, { method: "POST", body: { confirm: "DELETE" } });
    done++;
  }
  return done;
}

async function players(root, selected) {
  const search = el("input", { placeholder: "Ad ya da hesap no ile ara", value: sessionStorage.getItem("overseer.q") || "" });
  const list = el("div");
  const orphans = el("div");
  const drawer = el("div", { class: "drawer" });
  const chosen = new Set();
  const owner = can("owner");
  let rows = [];

  const deleteChosen = el("button", { class: "danger", onclick: async () => {
    const names = rows.filter((p) => chosen.has(p.accountId)).map((p) => p.displayName || p.accountId.slice(0, 8));
    if (!names.length) return toast("Önce silinecek hesapları seç.", true);
    if (!confirm(`${names.length} hesap kalıcı olarak silinecek (üs, binalar, ittifak üyeliği, giriş bağlantıları):\n\n${names.join("\n")}\n\nEmin misin?`)) return;
    try {
      const n = await deleteAccounts([...chosen]);
      toast(`${n} hesap silindi.`);
      chosen.clear();
      await load();
    } catch (e) { toast(e.message, true); }
  } }, "Seçilenleri sil");

  const selectTests = el("button", { onclick: () => {
    rows.filter(looksTest).forEach((p) => chosen.add(p.accountId));
    draw();
  } }, "Test görünenleri seç");

  function draw() {
    deleteChosen.textContent = chosen.size ? `Seçilenleri sil (${chosen.size})` : "Seçilenleri sil";
    list.replaceChildren(el("table", {},
      el("thead", {}, el("tr", {}, [owner ? "" : null, "Oyuncu", "Hesap no", "Durum", "Kayıt", "Son giriş", "Koruma", "Sürüm"].filter((h) => h !== null).map((h) => el("th", {}, h)))),
      el("tbody", {}, rows.map((p) => el("tr", { class: "click", onclick: () => (location.hash = "#/players/" + p.accountId) },
        owner ? el("td", { onclick: (e) => e.stopPropagation() }, el("input", { type: "checkbox", checked: chosen.has(p.accountId) ? "" : null,
          onchange: (e) => { e.target.checked ? chosen.add(p.accountId) : chosen.delete(p.accountId); draw(); } })) : null,
        el("td", {}, p.displayName || el("span", { class: "muted" }, "(adsız)"), looksTest(p) ? el("span", { class: "pill off" }, " test?") : ""),
        el("td", { class: "mono" }, p.accountId.slice(0, 12)),
        el("td", {}, p.bannedUntil && new Date(p.bannedUntil) > new Date() ? el("span", { class: "pill off" }, "Banlı")
          : p.online ? el("span", { class: "pill on" }, "Çevrimiçi") : el("span", { class: "muted" }, "Çevrimdışı")),
        el("td", {}, fmtDate(p.createdAt)),
        el("td", {}, fmtDate(p.lastLoginAt)),
        el("td", {}, p.providers.length ? p.providers.join(", ") : el("span", { class: "warn" }, "korumasız")),
        el("td", { class: "mono" }, p.clientVersion || "—"))))));
  }

  async function load() {
    sessionStorage.setItem("overseer.q", search.value);
    rows = await api("players?q=" + encodeURIComponent(search.value.trim()));
    draw();
    await loadOrphans();
  }

  // Player rows with no account at all (left by acceptance and load tests); nobody can sign in to them.
  async function loadOrphans() {
    const items = await api("orphans");
    if (!items.length) return orphans.replaceChildren();
    orphans.replaceChildren(el("div", { class: "card" },
      el("h2", {}, `Hesabı olmayan oyuncu kayıtları (${items.length})`),
      el("p", { class: "muted" }, "Hiçbir hesaba bağlı değiller; kimse bunlarla giriş yapamaz. Test kalıntılarıdır."),
      el("table", {}, el("tbody", {}, items.map((o) => el("tr", {}, el("td", { class: "mono" }, o.identityKey), el("td", {}, fmtDate(o.createdAt)))))),
      owner ? el("button", { class: "danger", onclick: async () => {
        if (!confirm(`${items.length} sahipsiz oyuncu kaydı kalıcı olarak silinecek. Emin misin?`)) return;
        try {
          const r = await api("orphans/delete", { method: "POST", body: { identityKeys: items.map((o) => o.identityKey), confirm: "DELETE" } });
          toast(`${r.deleted} kayıt silindi.`);
          await loadOrphans();
        } catch (e) { toast(e.message, true); }
      } }, "Hepsini sil") : null));
  }

  search.addEventListener("keydown", (e) => e.key === "Enter" && load());
  render(root, el("h1", {}, "Oyuncular"),
    el("div", { class: "row" }, search, el("button", { onclick: load }, "Ara"), owner ? selectTests : null, owner ? deleteChosen : null),
    drawer, list, orphans);
  await load();
  if (selected) await playerDetail(drawer, selected);
}

async function playerDetail(root, id) {
  const d = await api("players/" + id);
  const p = d.player;
  const banned = p.bannedUntil && new Date(p.bannedUntil) > new Date();
  const amounts = Object.fromEntries(RESOURCES.map((r) => [r, el("input", { type: "number", step: "1", placeholder: "0" })]));
  const note = el("input", { placeholder: "Not (destek talebi, sebep…)" });
  const banHours = el("input", { type: "number", min: "1", value: "24" });
  const banReason = el("input", { placeholder: "Sebep" });

  async function grant() {
    const body = { amounts: {}, note: note.value };
    for (const r of RESOURCES) if (amounts[r].value) body.amounts[r] = Number(amounts[r].value);
    await api(`players/${id}/grant`, { method: "POST", body });
    toast("Kaynaklar güncellendi.");
    route();
  }

  const actions = can("gm") ? [
    el("h2", {}, "Kaynak ver / al (eksi değer düşer)"),
    el("div", { class: "row" }, RESOURCES.map((r) => el("label", {}, RESOURCE_TR[r], amounts[r])), note,
      el("button", { class: "primary", onclick: () => grant().catch((e) => toast(e.message, true)) }, "Uygula")),
    el("h2", {}, "Yaptırım"),
    el("div", { class: "row" },
      banned
        ? el("button", { onclick: () => api(`players/${id}/unban`, { method: "POST" }).then(() => { toast("Ban kaldırıldı."); route(); }, (e) => toast(e.message, true)) }, "Banı kaldır")
        : [el("label", {}, "Süre (saat)", banHours), el("label", {}, "Sebep", banReason),
           el("button", { class: "danger", onclick: () => api(`players/${id}/ban`, { method: "POST", body: { hours: Number(banHours.value), reason: banReason.value } })
             .then(() => { toast("Oyuncu banlandı."); route(); }, (e) => toast(e.message, true)) }, "Banla")],
      el("button", { onclick: () => api(`players/${id}/kick`, { method: "POST" }).then((r) => toast(r.kicked ? "Oturum kapatıldı." : "Oyuncu çevrimdışı."), (e) => toast(e.message, true)) }, "Oturumu kapat"),
      el("button", { onclick: () => (location.hash = "#/coupons/" + id) }, "Bu oyuncuya kupon")),
    can("owner") ? [
      el("h2", {}, "Hesabı sil"),
      el("p", { class: "muted" }, "Test hesapları için: hesap, üs, binalar, ittifak üyeliği ve giriş bağlantıları kalıcı olarak silinir. Geri alınamaz."),
      el("button", { class: "danger", onclick: async () => {
        if (!confirm(`"${p.displayName || id}" hesabı kalıcı olarak silinecek. Emin misin?`)) return;
        try {
          await deleteAccounts([id]);
          toast("Hesap silindi.");
          location.hash = "#/players";
        } catch (e) { toast(e.message, true); }
      } }, "Hesabı kalıcı olarak sil")] : null,
  ] : [];

  render(root, el("div", { class: "card" },
    el("h1", {}, p.displayName, " ", el("span", { class: "muted mono" }, p.accountId)),
    el("p", { class: "muted" },
      `Kayıt ${fmtDate(p.createdAt)} · Son giriş ${fmtDate(p.lastLoginAt)} · ${p.online ? "Çevrimiçi" : "Çevrimdışı"}`,
      banned ? el("span", { class: "bad" }, ` · Banlı: ${fmtDate(p.bannedUntil)} (${p.banReason})`) : ""),
    el("div", { class: "grid2" },
      el("div", {}, el("h2", {}, "Kaynaklar"), el("table", {}, el("tbody", {},
        RESOURCES.map((r) => el("tr", {}, el("td", {}, RESOURCE_TR[r]), el("td", { class: "mono" }, fmtNum(d.wallet[r]))))))),
      el("div", {}, el("h2", {}, "Binalar"), el("table", {}, el("tbody", {},
        d.buildings.map((b) => el("tr", {}, el("td", {}, b.definitionId), el("td", {}, `Halka ${b.ring} · Yuva ${b.sector}`),
          el("td", {}, `Sv. ${b.level}`, b.constructionEndsAt ? el("span", { class: "warn" }, " · inşaatta") : ""))))))),
    actions));
}

async function coupons(root, targetId) {
  const rows = await api("coupons");
  const code = el("input", { placeholder: "Boş bırak: otomatik" });
  const rewards = Object.fromEntries(RESOURCES.map((r) => [r, el("input", { type: "number", min: "0", placeholder: "0" })]));
  const kind = el("select", {}, el("option", { value: "all" }, "Herkes"), el("option", { value: "one" }, "Tek oyuncu"));
  const target = el("input", { placeholder: "Oyuncunun hesap numarası", value: targetId || "" });
  const max = el("input", { type: "number", min: "0", value: "0" });
  const starts = el("input", { type: "datetime-local" });
  const ends = el("input", { type: "datetime-local" });
  const note = el("input", { placeholder: "Not (kampanya adı…)" });
  const targetRow = el("label", {}, "Oyuncu (hesap no)", target);
  if (targetId) kind.value = "one";
  const syncKind = () => targetRow.classList.toggle("hidden", kind.value !== "one");
  kind.addEventListener("change", syncKind);
  syncKind();

  async function create() {
    if (kind.value === "one" && !target.value.trim()) throw new Error("Tek oyunculuk kupon için hesap numarası gir.");
    const body = {
      code: code.value.trim(), rewards: {}, maxRedemptions: Number(max.value || 0), note: note.value,
      startsAt: starts.value ? new Date(starts.value).toISOString() : null,
      expiresAt: ends.value ? new Date(ends.value).toISOString() : null,
      targetAccountId: kind.value === "one" ? target.value.trim() : null,
    };
    for (const r of RESOURCES) if (Number(rewards[r].value) > 0) body.rewards[r] = Number(rewards[r].value);
    const created = await api("coupons", { method: "POST", body });
    toast("Kupon oluşturuldu: " + created.code);
    location.hash = "#/coupons";
    route();
  }

  const status = (c) => {
    const now = new Date();
    if (!c.active) return el("span", { class: "pill off" }, "Kapalı");
    if (c.startsAt && new Date(c.startsAt) > now) return el("span", { class: "pill" }, "Başlamadı");
    if (c.expiresAt && new Date(c.expiresAt) <= now) return el("span", { class: "pill off" }, "Süresi doldu");
    if (c.maxRedemptions > 0 && c.redeemed >= c.maxRedemptions) return el("span", { class: "pill off" }, "Limit doldu");
    return el("span", { class: "pill on" }, "Aktif");
  };

  render(root, 
    el("h1", {}, "Hediye Kuponları"),
    can("gm") ? el("div", { class: "card" },
      el("div", { class: "row" }, el("label", {}, "Kod", code), el("label", {}, "Kimin için", kind), targetRow,
        el("label", {}, "Toplam limit (0 = sınırsız)", max)),
      el("div", { class: "row" }, RESOURCES.map((r) => el("label", {}, RESOURCE_TR[r], rewards[r]))),
      el("div", { class: "row" }, el("label", {}, "Başlangıç (boş = hemen)", starts), el("label", {}, "Bitiş (boş = süresiz)", ends), el("label", {}, "Not", note),
        el("button", { class: "primary", onclick: () => create().catch((e) => toast(e.message, true)) }, "Kupon oluştur")),
      el("p", { class: "muted" }, "Her oyuncu bir kodu en fazla bir kez kullanabilir. Tek oyuncuya özel kodu başkası kullanamaz.")) : null,
    el("h2", {}, "Kuponlar"),
    el("table", {},
      el("thead", {}, el("tr", {}, ["Kod", "Ödül", "Kimin için", "Kullanım", "Geçerlilik", "Durum", "Not", ""].map((h) => el("th", {}, h)))),
      el("tbody", {}, rows.map((c) => el("tr", {},
        el("td", { class: "mono" }, c.code),
        el("td", {}, Object.entries(c.rewards).map(([r, v]) => `${fmtNum(v)} ${RESOURCE_TR[r] || r}`).join(", ")),
        el("td", { class: "mono" }, c.targetAccountId ? c.targetAccountId.slice(0, 12) : "Herkes"),
        el("td", {}, `${fmtNum(c.redeemed)} / ${c.maxRedemptions > 0 ? fmtNum(c.maxRedemptions) : "∞"}`),
        el("td", {}, `${c.startsAt ? fmtDate(c.startsAt) : "hemen"} → ${c.expiresAt ? fmtDate(c.expiresAt) : "süresiz"}`),
        el("td", {}, status(c)),
        el("td", { class: "muted" }, c.note),
        el("td", {}, can("gm") ? el("button", {
          onclick: () => api(`coupons/${c.code}/active`, { method: "POST", body: { active: !c.active } }).then(() => route(), (e) => toast(e.message, true)),
        }, c.active ? "Kapat" : "Aç") : ""))))));
}

async function liveops(root) {
  const s = await api("liveops");
  const maintenance = el("select", {}, el("option", { value: "false" }, "Kapalı"), el("option", { value: "true" }, "Açık"));
  maintenance.value = String(s.maintenance);
  const message = el("input", { value: s.maintenanceMessage, placeholder: "Bakım mesajı (oyuncular görür)" });
  const announcement = el("textarea", { class: "short" }, s.announcement);
  const latest = el("input", { value: s.latestClientVersion || "", placeholder: "ör. 1.2.0 (boş = sunucu ayarı)" });
  const minimum = el("input", { value: s.minClientVersion || "", placeholder: "ör. 1.1.0 (boş = sunucu ayarı)" });
  const updateUrl = el("input", { value: s.updateUrl || "", placeholder: "https://hadal.cicibyte.com/download/hadal.apk" });
  render(root, 
    el("h1", {}, "Canlı Operasyon"),
    el("div", { class: "card" },
      el("div", { class: "row" }, el("label", {}, "Bakım modu", maintenance), el("label", {}, "Bakım mesajı", message)),
      el("label", {}, "Açılış ekranı duyurusu (boş = yok)", announcement),
      el("p", { class: "muted" }, "Bakım açıkken yeni bağlantılar beklemeye alınır; değişiklik birkaç saniye içinde tüm istemcilere yansır."),
      el("h2", {}, "Uygulama güncellemesi (OTA)"),
      el("div", { class: "row" }, el("label", {}, "En yeni sürüm", latest), el("label", {}, "En düşük izinli sürüm", minimum)),
      el("label", {}, "İndirme adresi", updateUrl),
      el("p", { class: "muted" }, "En yeni sürümden eski oyunculara güncelleme önerilir; en düşük sürümün altındakiler güncellemeden oynayamaz. APK'yı önce kale'ye yükle, sonra sürümü burada yayınla."),
      can("gm") ? el("button", {
        class: "primary",
        onclick: () => api("liveops", { method: "PUT", body: {
          maintenance: maintenance.value === "true", maintenanceMessage: message.value, announcement: announcement.value,
          latestClientVersion: latest.value.trim(), minClientVersion: minimum.value.trim(), updateUrl: updateUrl.value.trim() } })
          .then(() => toast("Kaydedildi."), (e) => toast(e.message, true)),
      }, "Kaydet") : null));
}

async function balance(root) {
  const current = await api("catalog");
  const versions = await api("catalog/versions");
  const data = JSON.parse(current.json);
  const editable = can("owner");
  const num = (value, onChange, step = "1") => {
    const input = el("input", { type: "number", step, value, disabled: !editable });
    input.addEventListener("change", () => onChange(Number(input.value)));
    return input;
  };
  const amountCells = (list) => RESOURCES.map((r) => {
    const entry = (list || []).find((a) => a.resource === r);
    return el("td", {}, num(entry ? entry.amount : "", (v) => {
      const at = list.findIndex((a) => a.resource === r);
      if (!v) { if (at >= 0) list.splice(at, 1); } else if (at >= 0) list[at].amount = v; else list.push({ resource: r, amount: v });
    }));
  });

  const p = data.progression;
  const raw = el("textarea", {}, JSON.stringify(data, null, 2));
  const note = el("input", { placeholder: "Değişiklik notu (zorunlu değil)" });

  async function publish(json) {
    const result = await api("catalog", { method: "PUT", body: { json, note: note.value } });
    toast(`Yayınlandı: v${result.version}. Tüm oyunculara hemen uygulandı.`);
    route();
  }

  render(root, 
    el("h1", {}, "Denge · Seviyeler ", el("span", { class: "muted" }, `(yayındaki sürüm v${current.version})`)),
    editable ? null : el("p", { class: "warn" }, "Yalnızca 'Sahip' rolü değişiklik yayınlayabilir."),
    el("h2", {}, "Seviye ilerlemesi"),
    el("div", { class: "row" },
      el("label", {}, "En yüksek seviye", num(p.maxLevel, (v) => (p.maxLevel = v))),
      el("label", {}, "Maliyet çarpanı / seviye", num(p.costGrowth, (v) => (p.costGrowth = v), "0.01")),
      el("label", {}, "Süre çarpanı / seviye", num(p.timeGrowth, (v) => (p.timeGrowth = v), "0.01")),
      el("label", {}, "Ücretsiz bitirme (sn)", num(p.freeFinishSeconds, (v) => (p.freeFinishSeconds = v))),
      el("label", {}, "İnşaatçı sayısı", num(p.builderSlots, (v) => (p.builderSlots = v)))),
    el("h2", {}, "Binalar · 1. seviye maliyeti ve süresi"),
    el("table", {},
      el("thead", {}, el("tr", {}, el("th", {}, "Bina"), el("th", {}, "Süre (sn)"), RESOURCES.map((r) => el("th", {}, RESOURCE_TR[r])))),
      el("tbody", {}, data.buildings.map((b) => {
        b.buildCost = b.buildCost || [];
        return el("tr", {}, el("td", {}, b.displayName), el("td", {}, num(b.buildSeconds || 0, (v) => (b.buildSeconds = v))), amountCells(b.buildCost));
      }))),
    el("h2", {}, "Binalar · üretim (tik başına, 1. seviye)"),
    el("table", {},
      el("thead", {}, el("tr", {}, el("th", {}, "Bina"), RESOURCES.map((r) => el("th", {}, RESOURCE_TR[r])))),
      el("tbody", {}, data.buildings.map((b) => {
        b.production = b.production || [];
        return el("tr", {}, el("td", {}, b.displayName), amountCells(b.production));
      }))),
    editable ? el("div", { class: "row" }, note,
      el("button", { class: "primary", onclick: () => publish(JSON.stringify(data, null, 2)).catch((e) => toast(e.message, true)) }, "Tablodaki değişiklikleri yayınla")) : null,
    el("h2", {}, "Gelişmiş: ham katalog JSON"),
    raw,
    editable ? el("div", { class: "row" },
      el("button", { onclick: () => publish(raw.value).catch((e) => toast(e.message, true)) }, "JSON'u yayınla")) : null,
    el("h2", {}, "Sürüm geçmişi"),
    el("table", {},
      el("thead", {}, el("tr", {}, ["Sürüm", "Tarih", "Yayınlayan", "Not", ""].map((h) => el("th", {}, h)))),
      el("tbody", {}, versions.map((v) => el("tr", {},
        el("td", {}, "v" + v.version), el("td", {}, fmtDate(v.createdAt)), el("td", {}, v.createdBy), el("td", { class: "muted" }, v.note),
        el("td", {}, editable && v.version !== current.version ? el("button", {
          onclick: () => api(`catalog/${v.version}/restore`, { method: "POST" }).then((r) => { toast(`v${v.version} geri yüklendi (yeni sürüm v${r.version}).`); route(); }, (e) => toast(e.message, true)),
        }, "Geri yükle") : ""))))));
}

async function chat(root) {
  const rows = await api("chat?channel=world");
  render(root, el("h1", {}, "Sohbet · Dünya kanalı"),
    el("p", { class: "muted" }, "Son 50 mesaj, en yenisi üstte. Silinen mesaj tüm oyuncuların ekranından kalkar."),
    el("table", {},
      el("thead", {}, el("tr", {}, ["Zaman", "Oyuncu", "Mesaj", ""].map((h) => el("th", {}, h)))),
      el("tbody", {}, rows.map((m) => el("tr", {},
        el("td", {}, fmtDate(m.sentAt)),
        el("td", {}, (m.allianceTag ? `[${m.allianceTag}] ` : "") + (m.senderName || "sistem")),
        el("td", {}, m.text),
        el("td", {}, el("button", {
          onclick: () => api(`chat/${m.id}`, { method: "DELETE" }).then(() => { toast("Mesaj silindi."); route(); }, (e) => toast(e.message, true)),
        }, "Sil")))))));
}

async function audit(root) {
  const rows = await api("audit");
  render(root, el("h1", {}, "Denetim Kaydı"), el("table", {},
    el("thead", {}, el("tr", {}, ["Zaman", "Yönetici", "İşlem", "Hedef", "Ayrıntı", "IP"].map((h) => el("th", {}, h)))),
    el("tbody", {}, rows.map((e) => el("tr", {},
      el("td", {}, fmtDate(e.at)), el("td", {}, e.admin), el("td", { class: e.action.endsWith("failed") ? "bad" : "" }, e.action),
      el("td", { class: "mono" }, e.target), el("td", { class: "muted" }, e.details), el("td", { class: "mono" }, e.ip))))));
}

async function admins(root) {
  const rows = await api("admins");
  const username = el("input", { placeholder: "kullanıcı adı" });
  const role = el("select", {}, Object.entries(ROLE_TR).map(([k, v]) => el("option", { value: k }, v)));
  role.value = "gm";
  const result = el("div");
  render(root, 
    el("h1", {}, "Yöneticiler"),
    el("div", { class: "card" }, el("div", { class: "row" }, el("label", {}, "Kullanıcı adı", username), el("label", {}, "Rol", role),
      el("button", {
        class: "primary",
        onclick: () => api("admins", { method: "POST", body: { username: username.value.trim(), role: role.value } }).then((r) => {
          result.replaceChildren(el("div", { class: "secret" },
            el("div", {}, "Bu bilgiler YALNIZCA BİR KEZ gösterilir. Güvenli bir kanalla ilet:"),
            el("div", {}, `Kullanıcı: ${r.username}`), el("div", {}, `Şifre: ${r.password}`),
            r.totpSecret ? el("div", {}, `2FA anahtarı: ${r.totpSecret}`) : null));
        }, (e) => toast(e.message, true)),
      }, "Yönetici ekle")), result),
    el("table", {},
      el("thead", {}, el("tr", {}, ["Kullanıcı", "Rol", "Durum", "Son giriş", ""].map((h) => el("th", {}, h)))),
      el("tbody", {}, rows.map((a) => el("tr", {},
        el("td", {}, a.username), el("td", {}, ROLE_TR[a.role] || a.role),
        el("td", {}, a.disabled ? el("span", { class: "pill off" }, "Kapalı") : el("span", { class: "pill on" }, "Aktif")),
        el("td", {}, fmtDate(a.lastLoginAt)),
        el("td", {}, a.username === state.user.username ? "" : el("button", {
          onclick: () => api(`admins/${a.id}/disabled`, { method: "POST", body: { disabled: !a.disabled } }).then(() => route(), (e) => toast(e.message, true)),
        }, a.disabled ? "Aç" : "Kapat")))))));
}

// ---------------------------------------------------------------- Boot
$("#login-form").addEventListener("submit", login);
$("#logout").addEventListener("click", logout);
window.addEventListener("hashchange", route);
start();
