namespace Hadal.Game.PushNotifications
{
    /// <summary>
    /// Push texts in the game's 18 languages (the player's choice travels with NotificationPrefsDto.Language; English
    /// when unknown). {0} is a player name; the phone shows the title in bold and the body under it.
    /// </summary>
    public static class PushTexts
    {
        private static readonly string[] Order =
            { "AttackIncoming", "AttackResult", "Mail", "AllianceHelp", "RallyStarted", "ChatMention" };

        // Per language: title, body for each kind in Order.
        private static readonly Dictionary<string, string[]> Texts = new(StringComparer.Ordinal)
        {
            ["en"] = new[]
            {
                "Attack incoming!", "{0} is attacking your base. Raise a shield or call for help.",
                "Battle report", "A battle is over. Open the game to see the report.",
                "New mail", "You have a new mail from {0}.",
                "Alliance help", "{0} asked the alliance for help.",
                "Rally started", "{0} started a rally. Join before it departs!",
                "New message", "{0} mentioned you in chat."
            },
            ["tr"] = new[]
            {
                "Saldırı geliyor!", "{0} üssüne saldırıyor. Kalkan kaldır ya da yardım iste.",
                "Savaş raporu", "Bir savaş bitti. Raporu görmek için oyunu aç.",
                "Yeni posta", "{0} sana bir posta gönderdi.",
                "İttifak yardımı", "{0} ittifaktan yardım istedi.",
                "Ralli başladı", "{0} bir ralli başlattı. Yola çıkmadan katıl!",
                "Yeni mesaj", "{0} sohbette senden bahsetti."
            },
            ["de"] = new[]
            {
                "Angriff im Anmarsch!", "{0} greift deine Basis an. Aktiviere einen Schild oder rufe um Hilfe.",
                "Kampfbericht", "Ein Kampf ist vorbei. Öffne das Spiel, um den Bericht zu sehen.",
                "Neue Post", "Du hast neue Post von {0}.",
                "Allianzhilfe", "{0} hat die Allianz um Hilfe gebeten.",
                "Sammelangriff gestartet", "{0} hat einen Sammelangriff gestartet. Schließ dich an, bevor er aufbricht!",
                "Neue Nachricht", "{0} hat dich im Chat erwähnt."
            },
            ["fr"] = new[]
            {
                "Attaque en approche !", "{0} attaque ta base. Active un bouclier ou appelle à l'aide.",
                "Rapport de bataille", "Une bataille est terminée. Ouvre le jeu pour voir le rapport.",
                "Nouveau courrier", "Tu as reçu un courrier de {0}.",
                "Aide d'alliance", "{0} a demandé de l'aide à l'alliance.",
                "Ralliement lancé", "{0} a lancé un ralliement. Rejoins-le avant le départ !",
                "Nouveau message", "{0} t'a mentionné dans le chat."
            },
            ["es"] = new[]
            {
                "¡Ataque en camino!", "{0} está atacando tu base. Activa un escudo o pide ayuda.",
                "Informe de batalla", "Una batalla ha terminado. Abre el juego para ver el informe.",
                "Correo nuevo", "Tienes un correo nuevo de {0}.",
                "Ayuda de alianza", "{0} ha pedido ayuda a la alianza.",
                "Concentración iniciada", "{0} ha iniciado una concentración. ¡Únete antes de que parta!",
                "Mensaje nuevo", "{0} te ha mencionado en el chat."
            },
            ["it"] = new[]
            {
                "Attacco in arrivo!", "{0} sta attaccando la tua base. Attiva uno scudo o chiedi aiuto.",
                "Rapporto di battaglia", "Una battaglia è finita. Apri il gioco per vedere il rapporto.",
                "Nuova posta", "Hai una nuova lettera da {0}.",
                "Aiuto dell'alleanza", "{0} ha chiesto aiuto all'alleanza.",
                "Raduno iniziato", "{0} ha avviato un raduno. Unisciti prima che parta!",
                "Nuovo messaggio", "{0} ti ha menzionato in chat."
            },
            ["pt"] = new[]
            {
                "Ataque a caminho!", "{0} está atacando sua base. Ative um escudo ou peça ajuda.",
                "Relatório de batalha", "Uma batalha terminou. Abra o jogo para ver o relatório.",
                "Novo correio", "Você tem um novo correio de {0}.",
                "Ajuda da aliança", "{0} pediu ajuda à aliança.",
                "Reunião iniciada", "{0} iniciou uma reunião. Junte-se antes da partida!",
                "Nova mensagem", "{0} mencionou você no chat."
            },
            ["nl"] = new[]
            {
                "Aanval onderweg!", "{0} valt je basis aan. Activeer een schild of vraag om hulp.",
                "Gevechtsrapport", "Een gevecht is voorbij. Open het spel om het rapport te zien.",
                "Nieuwe post", "Je hebt nieuwe post van {0}.",
                "Alliantiehulp", "{0} heeft de alliantie om hulp gevraagd.",
                "Verzamelaanval gestart", "{0} is een verzamelaanval begonnen. Doe mee voor vertrek!",
                "Nieuw bericht", "{0} heeft je genoemd in de chat."
            },
            ["ru"] = new[]
            {
                "Атака приближается!", "{0} атакует вашу базу. Включите щит или позовите на помощь.",
                "Отчёт о бое", "Бой окончен. Откройте игру, чтобы увидеть отчёт.",
                "Новое письмо", "Вам пришло письмо от {0}.",
                "Помощь альянса", "{0} просит альянс о помощи.",
                "Сбор начат", "{0} начал сбор. Присоединяйтесь до выступления!",
                "Новое сообщение", "{0} упомянул вас в чате."
            },
            ["ar"] = new[]
            {
                "هجوم قادم!", "{0} يهاجم قاعدتك. فعّل درعًا أو اطلب المساعدة.",
                "تقرير المعركة", "انتهت معركة. افتح اللعبة لرؤية التقرير.",
                "بريد جديد", "لديك بريد جديد من {0}.",
                "مساعدة التحالف", "طلب {0} المساعدة من التحالف.",
                "بدأ الحشد", "بدأ {0} حشدًا. انضم قبل انطلاقه!",
                "رسالة جديدة", "ذكرك {0} في الدردشة."
            },
            ["ja"] = new[]
            {
                "攻撃が迫っています！", "{0}があなたの基地を攻撃しています。シールドを張るか救援を呼びましょう。",
                "戦闘レポート", "戦闘が終わりました。ゲームを開いてレポートを確認しましょう。",
                "新しいメール", "{0}から新しいメールが届きました。",
                "同盟の支援要請", "{0}が同盟に支援を求めています。",
                "集結開始", "{0}が集結を開始しました。出発前に参加しましょう！",
                "新しいメッセージ", "{0}がチャットであなたをメンションしました。"
            },
            ["ko"] = new[]
            {
                "공격이 다가옵니다!", "{0}님이 기지를 공격하고 있습니다. 보호막을 켜거나 도움을 요청하세요.",
                "전투 보고서", "전투가 끝났습니다. 게임을 열어 보고서를 확인하세요.",
                "새 우편", "{0}님이 우편을 보냈습니다.",
                "연맹 지원", "{0}님이 연맹에 도움을 요청했습니다.",
                "집결 시작", "{0}님이 집결을 시작했습니다. 출발 전에 참여하세요!",
                "새 메시지", "{0}님이 채팅에서 회원님을 언급했습니다."
            },
            ["zh-Hans"] = new[]
            {
                "敌军来袭！", "{0}正在进攻你的基地。开启护盾或请求支援。",
                "战斗报告", "一场战斗已结束。打开游戏查看报告。",
                "新邮件", "你收到了来自{0}的新邮件。",
                "联盟求助", "{0}向联盟请求帮助。",
                "集结已开始", "{0}发起了集结。在出发前加入吧！",
                "新消息", "{0}在聊天中提到了你。"
            },
            ["zh-Hant"] = new[]
            {
                "敵軍來襲！", "{0}正在進攻你的基地。開啟護盾或請求支援。",
                "戰鬥報告", "一場戰鬥已結束。打開遊戲查看報告。",
                "新郵件", "你收到了來自{0}的新郵件。",
                "聯盟求助", "{0}向聯盟請求幫助。",
                "集結已開始", "{0}發起了集結。在出發前加入吧！",
                "新訊息", "{0}在聊天中提到了你。"
            },
            ["th"] = new[]
            {
                "การโจมตีกำลังมา!", "{0} กำลังโจมตีฐานของคุณ เปิดโล่หรือขอความช่วยเหลือ",
                "รายงานการรบ", "การรบสิ้นสุดแล้ว เปิดเกมเพื่อดูรายงาน",
                "จดหมายใหม่", "คุณมีจดหมายใหม่จาก {0}",
                "ความช่วยเหลือพันธมิตร", "{0} ขอความช่วยเหลือจากพันธมิตร",
                "เริ่มการรวมพล", "{0} เริ่มการรวมพลแล้ว เข้าร่วมก่อนออกเดินทาง!",
                "ข้อความใหม่", "{0} กล่าวถึงคุณในแชท"
            },
            ["vi"] = new[]
            {
                "Sắp bị tấn công!", "{0} đang tấn công căn cứ của bạn. Bật khiên hoặc kêu gọi trợ giúp.",
                "Báo cáo trận đánh", "Một trận đánh đã kết thúc. Mở trò chơi để xem báo cáo.",
                "Thư mới", "Bạn có thư mới từ {0}.",
                "Liên minh trợ giúp", "{0} đã nhờ liên minh trợ giúp.",
                "Tập hợp bắt đầu", "{0} đã bắt đầu tập hợp. Hãy tham gia trước khi xuất phát!",
                "Tin nhắn mới", "{0} đã nhắc đến bạn trong trò chuyện."
            },
            ["id"] = new[]
            {
                "Serangan datang!", "{0} menyerang markasmu. Aktifkan perisai atau minta bantuan.",
                "Laporan pertempuran", "Sebuah pertempuran telah selesai. Buka game untuk melihat laporannya.",
                "Surat baru", "Kamu mendapat surat baru dari {0}.",
                "Bantuan aliansi", "{0} meminta bantuan aliansi.",
                "Rally dimulai", "{0} memulai rally. Bergabunglah sebelum berangkat!",
                "Pesan baru", "{0} menyebutmu di obrolan."
            },
            ["ms"] = new[]
            {
                "Serangan menghampiri!", "{0} sedang menyerang pangkalan anda. Aktifkan perisai atau minta bantuan.",
                "Laporan pertempuran", "Satu pertempuran telah tamat. Buka permainan untuk melihat laporan.",
                "Mel baharu", "Anda menerima mel baharu daripada {0}.",
                "Bantuan pakatan", "{0} meminta bantuan daripada pakatan.",
                "Rali bermula", "{0} memulakan rali. Sertai sebelum ia bertolak!",
                "Mesej baharu", "{0} menyebut anda dalam sembang."
            }
        };

        public static IReadOnlyCollection<string> Languages => Texts.Keys;

        /// <summary>Title and body of one push in a language (English when the language is unknown).</summary>
        public static (string Title, string Body) For(string? language, PushKind kind, string arg)
        {
            var texts = Texts.TryGetValue(language ?? string.Empty, out var found) ? found : Texts["en"];
            var index = Array.IndexOf(Order, kind.ToString());
            var name = string.IsNullOrWhiteSpace(arg) ? "HADAL" : arg.Trim();
            if (name.Length > 40)
                name = name[..40];
            return (texts[index * 2], string.Format(texts[index * 2 + 1], name));
        }
    }
}
