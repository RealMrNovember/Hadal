using HADAL.Shared.Simulation;

namespace Hadal.Game.Events
{
    /// <summary>
    /// The ready-made calendar (roadmap §7, docs/29 §8.1), seeded once into an empty calendar so that without any Overseer
    /// work there is a timed event every day and an alliance goal every week. Everything here stays editable and can be
    /// switched off in the panel. All times are server time (UTC). Rewards are catalog resources and bag items, kept
    /// modest: the daily race pays less per day than the hand-made week-long contests of the panel's presets.
    /// </summary>
    public static class DefaultSchedules
    {
        public const string DepthRace = "depth-race";
        public const string AllianceWeek = "alliance-week";
        public const string LoginWeek = "login-week";

        private static readonly int[] EveryDay = { 0, 1, 2, 3, 4, 5, 6 };
        private const int Monday = (int)DayOfWeek.Monday;

        public static List<EventSchedule> All(DateTimeOffset now) => new() { Depth(now), Alliance(now), Login(now) };

        /// <summary>
        /// Depth Race (docs/29 §4.4): every day 00:00–24:00 UTC, six 4-hour themes from the existing score kinds (gathering,
        /// building power, research, training, creature hunting, hero growth), rotated one slot a day; stage chests and a
        /// personal ranking per kingdom.
        /// </summary>
        private static EventSchedule Depth(DateTimeOffset now) => new()
        {
            Id = DepthRace,
            Weekdays = EveryDay.ToList(),
            StartMinute = 0,
            DurationHours = 24,
            LeadHours = 24,
            RotatePhases = true,
            Note = "Default: daily themed solo race",
            CreatedBy = EventService.CalendarAuthor,
            CreatedAt = now,
            UpdatedAt = now,
            Blueprint = new EventDefinition
            {
                Template = EventTemplates.Points,
                Banner = "abyss",
                ClaimDays = 1,
                MinRankPoints = 600,
                Title = T("Derinlik Yarışı", "Depth Race", "Tiefenrennen", "Course des profondeurs", "Carrera de las profundidades", "Corsa degli abissi",
                    "Corrida das profundezas", "Dieptewedstrijd", "Гонка глубин", "سباق الأعماق", "深淵レース", "심해 레이스", "深渊竞赛", "深淵競賽",
                    "การแข่งขันใต้ทะเลลึก", "Cuộc đua vực sâu", "Balapan Kedalaman", "Perlumbaan Kedalaman"),
                Description = T(
                    "Her 4 saatte yeni bir tema: her temada puan topla, aşama sandıklarını aç ve krallığının günlük sıralamasında yüksel.",
                    "A new theme every 4 hours: score in each one, open the stage chests and climb your kingdom's daily ranking.",
                    "Alle 4 Stunden ein neues Thema: Sammle in jedem Punkte, öffne die Stufentruhen und steige in der Tageswertung deines Königreichs auf.",
                    "Un nouveau thème toutes les 4 heures : marquez des points dans chacun, ouvrez les coffres de palier et grimpez dans le classement quotidien de votre royaume.",
                    "Un tema nuevo cada 4 horas: puntúa en cada uno, abre los cofres de etapa y sube en la clasificación diaria de tu reino.",
                    "Un nuovo tema ogni 4 ore: fai punti in ciascuno, apri i forzieri delle fasi e scala la classifica giornaliera del tuo regno.",
                    "Um novo tema a cada 4 horas: pontue em cada um, abra os baús de etapa e suba no ranking diário do seu reino.",
                    "Elke 4 uur een nieuw thema: scoor in elk thema, open de etappekisten en klim in de dagranglijst van je koninkrijk.",
                    "Каждые 4 часа новая тема: набирайте очки в каждой, открывайте сундуки этапов и поднимайтесь в дневном рейтинге своего королевства.",
                    "موضوع جديد كل 4 ساعات: اجمع النقاط في كل موضوع، وافتح صناديق المراحل، وتقدّم في الترتيب اليومي لمملكتك.",
                    "4時間ごとに新しいテーマ。各テーマでポイントを稼ぎ、段階報酬の宝箱を開けて、王国の日間ランキングを駆け上がろう。",
                    "4시간마다 새로운 테마! 테마마다 점수를 모아 단계 상자를 열고 왕국 일일 순위를 올리세요.",
                    "每4小时更换一个主题：在每个主题中得分，打开阶段宝箱，在王国每日排行榜中攀升。",
                    "每4小時更換一個主題：在每個主題中得分，打開階段寶箱，在王國每日排行榜中攀升。",
                    "ธีมใหม่ทุก 4 ชั่วโมง: เก็บแต้มในแต่ละธีม เปิดหีบรางวัลขั้น และไต่อันดับรายวันของอาณาจักรคุณ",
                    "Cứ 4 giờ lại có chủ đề mới: ghi điểm ở mỗi chủ đề, mở rương mốc và leo bảng xếp hạng ngày của vương quốc.",
                    "Tema baru setiap 4 jam: kumpulkan poin di setiap tema, buka peti tahap, dan naiki peringkat harian kerajaanmu.",
                    "Tema baharu setiap 4 jam: kumpul mata dalam setiap tema, buka peti peringkat dan naik dalam ranking harian kerajaan anda."),
                Phases =
                {
                    Phase(0, T("Kaynak Akını", "Resource Rush", "Rohstoffansturm", "Ruée vers les ressources", "Fiebre de recursos", "Corsa alle risorse",
                            "Corrida aos recursos", "Grondstoffenrush", "Сбор ресурсов", "اندفاع الموارد", "資源ラッシュ", "자원 러시", "资源冲刺", "資源衝刺",
                            "เร่งเก็บทรัพยากร", "Cơn sốt tài nguyên", "Buru Sumber Daya", "Rebut Sumber"),
                        A(EventActions.Gather, 1, 1000)),
                    Phase(4, T("Üs Genişlemesi", "Base Expansion", "Basisausbau", "Expansion de la base", "Expansión de la base", "Espansione della base",
                            "Expansão da base", "Basisuitbreiding", "Расширение базы", "توسيع القاعدة", "基地拡張", "기지 확장", "基地扩张", "基地擴張",
                            "ขยายฐาน", "Mở rộng căn cứ", "Perluasan Markas", "Pembesaran Pangkalan"),
                        A(EventActions.BuildPower, 2), A(EventActions.SpeedupMinutes, 1)),
                    Phase(8, T("Bilim Dalışı", "Science Dive", "Forschungstauchgang", "Plongée scientifique", "Inmersión científica", "Immersione scientifica",
                            "Mergulho científico", "Wetenschapsduik", "Научное погружение", "الغوص العلمي", "科学ダイブ", "과학 다이브", "科学深潜", "科學深潛",
                            "ดำดิ่งวิทยาศาสตร์", "Lặn khoa học", "Selam Sains", "Selaman Sains"),
                        A(EventActions.Research, 300), A(EventActions.SpeedupMinutes, 1)),
                    Phase(12, T("Ordu Kur", "Raise the Army", "Armee aufstellen", "Lever l'armée", "Forma el ejército", "Arruola l'esercito",
                            "Erga o exército", "Leger op de been", "Собери армию", "جهّز الجيش", "軍備増強", "군대 양성", "组建军队", "組建軍隊",
                            "สร้างกองทัพ", "Xây dựng quân đội", "Bangun Pasukan", "Bina Tentera"),
                        A(EventActions.Train(1), 1), A(EventActions.Train(2), 2), A(EventActions.Train(3), 4), A(EventActions.Train(4), 8), A(EventActions.Train(5), 16)),
                    Phase(16, T("Yaratık Avı", "Creature Hunt", "Kreaturenjagd", "Chasse aux créatures", "Caza de criaturas", "Caccia alle creature",
                            "Caça às criaturas", "Wezensjacht", "Охота на существ", "صيد المخلوقات", "クリーチャー狩り", "괴수 사냥", "狩猎生物", "狩獵生物",
                            "ล่าสัตว์ประหลาด", "Săn sinh vật", "Perburuan Makhluk", "Buruan Makhluk"),
                        A(EventActions.MonsterKill, 100), A(EventActions.MonsterLevels, 20)),
                    Phase(20, T("Kaşifleri Güçlendir", "Empower the Divers", "Taucher stärken", "Renforcer les plongeurs", "Fortalece a los buzos", "Potenzia i sommozzatori",
                            "Fortaleça os mergulhadores", "Versterk de duikers", "Усиль ныряльщиков", "قوِّ الغواصين", "ダイバー強化", "다이버 강화", "强化潜航员", "強化潛航員",
                            "เสริมพลังนักดำน้ำ", "Tăng sức thợ lặn", "Perkuat Penyelam", "Perkasa Penyelam"),
                        A(EventActions.HeroLevel, 50), A(EventActions.HeroPull, 200), A(EventActions.HeroRecruit, 300))
                },
                Stages =
                {
                    Stage(600, Res("Titanium", 5000), Item("speed_any_5", 2)),
                    Stage(2500, Res("Titanium", 15000), Item("chest_bronze", 1)),
                    Stage(7000, Item("chest_silver", 1), Item("speed_any_60", 1))
                },
                RankBands =
                {
                    Band(1, 1, Res("Hadalite", 100), Item("chest_gold", 1)),
                    Band(2, 3, Res("Hadalite", 50), Item("chest_silver", 1)),
                    Band(4, 10, Res("Hadalite", 20), Item("chest_bronze", 2)),
                    Band(11, 30, Item("chest_bronze", 1))
                }
            }
        };

        /// <summary>The weekly alliance race: Monday 00:00 UTC for a week, every main score kind, alliances ranked per kingdom.</summary>
        private static EventSchedule Alliance(DateTimeOffset now) => new()
        {
            Id = AllianceWeek,
            Weekdays = new List<int> { Monday },
            StartMinute = 0,
            DurationHours = 7 * 24,
            LeadHours = 24,
            Note = "Default: weekly alliance race",
            CreatedBy = EventService.CalendarAuthor,
            CreatedAt = now,
            UpdatedAt = now,
            Blueprint = new EventDefinition
            {
                Template = EventTemplates.Alliance,
                Banner = "vent",
                ClaimDays = 2,
                MinRankPoints = 1000,
                Title = T("İttifak Haftası", "Alliance Week", "Allianzwoche", "Semaine d'alliance", "Semana de la alianza", "Settimana dell'alleanza",
                    "Semana da aliança", "Alliantieweek", "Неделя альянсов", "أسبوع التحالف", "同盟ウィーク", "연맹 주간", "联盟周", "聯盟週",
                    "สัปดาห์พันธมิตร", "Tuần liên minh", "Pekan Aliansi", "Minggu Pakatan"),
                Description = T(
                    "Bütün hafta boyunca her üyenin puanı ittifaka yazılır: topla, inşa et, araştır, eğit, avla, kahramanlarını geliştir. İttifaklar her krallıkta kendi aralarında sıralanır.",
                    "All week long every member's points count for the alliance: gather, build, research, train, hunt and grow heroes. Alliances are ranked within each kingdom.",
                    "Die ganze Woche zählen die Punkte jedes Mitglieds für die Allianz: sammeln, bauen, forschen, ausbilden, jagen und Helden stärken. Allianzen werden je Königreich gewertet.",
                    "Toute la semaine, les points de chaque membre comptent pour l'alliance : récolter, construire, rechercher, entraîner, chasser et faire progresser les héros. Les alliances sont classées par royaume.",
                    "Durante toda la semana, los puntos de cada miembro cuentan para la alianza: recolecta, construye, investiga, entrena, caza y mejora héroes. Las alianzas se clasifican por reino.",
                    "Per tutta la settimana i punti di ogni membro valgono per l'alleanza: raccogli, costruisci, ricerca, addestra, caccia e potenzia gli eroi. Le alleanze sono classificate per regno.",
                    "A semana toda, os pontos de cada membro contam para a aliança: colete, construa, pesquise, treine, cace e evolua heróis. As alianças são classificadas por reino.",
                    "De hele week tellen de punten van elk lid voor de alliantie: verzamelen, bouwen, onderzoeken, trainen, jagen en helden versterken. Allianties worden per koninkrijk gerangschikt.",
                    "Всю неделю очки каждого участника идут в зачёт альянса: добывайте, стройте, исследуйте, обучайте войска, охотьтесь и развивайте героев. Альянсы ранжируются в пределах королевства.",
                    "طوال الأسبوع تُحتسب نقاط كل عضو للتحالف: اجمع وابنِ وابحث ودرّب واصطد وطوّر الأبطال. تُرتَّب التحالفات داخل كل مملكة.",
                    "1週間、メンバー全員のポイントが同盟に加算されます。採集・建設・研究・訓練・狩り・英雄育成で稼ごう。同盟は王国ごとにランク付けされます。",
                    "일주일 내내 모든 멤버의 점수가 연맹에 합산됩니다. 채집, 건설, 연구, 훈련, 사냥, 영웅 육성으로 점수를 모으세요. 연맹 순위는 왕국별로 매겨집니다.",
                    "整整一周，每位成员的积分都计入联盟：采集、建造、研究、训练、狩猎和培养英雄。联盟在各自王国内排名。",
                    "整整一週，每位成員的積分都計入聯盟：採集、建造、研究、訓練、狩獵和培養英雄。聯盟在各自王國內排名。",
                    "ตลอดทั้งสัปดาห์ แต้มของสมาชิกทุกคนนับให้พันธมิตร: เก็บทรัพยากร สร้าง วิจัย ฝึกทหาร ล่า และพัฒนาฮีโร่ พันธมิตรจะถูกจัดอันดับภายในแต่ละอาณาจักร",
                    "Suốt cả tuần, điểm của mỗi thành viên được tính cho liên minh: thu thập, xây dựng, nghiên cứu, huấn luyện, săn bắt và nâng cấp anh hùng. Liên minh được xếp hạng trong từng vương quốc.",
                    "Sepanjang minggu, poin setiap anggota dihitung untuk aliansi: kumpulkan, bangun, riset, latih, berburu, dan kembangkan pahlawan. Aliansi diperingkat di setiap kerajaan.",
                    "Sepanjang minggu, mata setiap ahli dikira untuk pakatan: kumpul, bina, kaji, latih, buru dan tingkatkan wira. Pakatan disusun mengikut kedudukan dalam setiap kerajaan."),
                Actions =
                {
                    A(EventActions.Gather, 1, 1000), A(EventActions.BuildPower, 1), A(EventActions.Research, 150),
                    A(EventActions.Train(1), 1), A(EventActions.Train(2), 2), A(EventActions.Train(3), 4), A(EventActions.Train(4), 8), A(EventActions.Train(5), 16),
                    A(EventActions.MonsterKill, 50), A(EventActions.MonsterLevels, 10), A(EventActions.HeroLevel, 25), A(EventActions.HeroPull, 100),
                    A(EventActions.Help, 10)
                },
                Stages =
                {
                    Stage(3000, Res("Titanium", 15000)),
                    Stage(12000, Item("chest_silver", 1)),
                    Stage(30000, Item("chest_gold", 1))
                },
                RankBands =
                {
                    Band(1, 1, Res("Hadalite", 200), Item("chest_gold", 1)),
                    Band(2, 3, Res("Hadalite", 100), Item("chest_silver", 1)),
                    Band(4, 10, Res("Hadalite", 40), Item("chest_bronze", 2))
                }
            }
        };

        /// <summary>Seven-day sign-in rewards, starting again every Monday 00:00 UTC.</summary>
        private static EventSchedule Login(DateTimeOffset now) => new()
        {
            Id = LoginWeek,
            Weekdays = new List<int> { Monday },
            StartMinute = 0,
            DurationHours = 7 * 24,
            LeadHours = 24,
            Note = "Default: weekly 7-day sign-in rewards",
            CreatedBy = EventService.CalendarAuthor,
            CreatedAt = now,
            UpdatedAt = now,
            Blueprint = new EventDefinition
            {
                Template = EventTemplates.Login,
                Banner = "wreck",
                ClaimDays = 1,
                Title = T("Dalış Günlüğü", "Dive Log", "Tauchlogbuch", "Journal de plongée", "Bitácora de inmersión", "Diario d'immersione",
                    "Diário de mergulho", "Duiklogboek", "Журнал погружений", "سجل الغوص", "ダイブ日誌", "다이브 일지", "潜航日志", "潛航日誌",
                    "บันทึกการดำน้ำ", "Nhật ký lặn", "Log Penyelaman", "Log Selaman"),
                Description = T(
                    "Haftanın her günü giriş yap ve o günün ödülünü al; en iyisi yedinci günde. Her pazartesi yeni bir günlük başlar.",
                    "Sign in on each day of the week to collect that day's reward; the seventh holds the best one. A new log starts every Monday.",
                    "Melde dich an jedem Tag der Woche an und hol dir die Tagesbelohnung; am siebten wartet die beste. Jeden Montag beginnt ein neues Logbuch.",
                    "Connectez-vous chaque jour de la semaine pour récupérer la récompense du jour ; la meilleure attend le septième. Un nouveau journal commence chaque lundi.",
                    "Conéctate cada día de la semana para recoger la recompensa del día; la mejor llega el séptimo. Cada lunes empieza una bitácora nueva.",
                    "Accedi ogni giorno della settimana per ritirare la ricompensa del giorno; la migliore arriva il settimo. Ogni lunedì inizia un nuovo diario.",
                    "Entre em cada dia da semana para receber a recompensa do dia; a melhor fica no sétimo. Toda segunda-feira começa um novo diário.",
                    "Log elke dag van de week in voor de beloning van die dag; de beste wacht op dag zeven. Elke maandag begint een nieuw logboek.",
                    "Заходите в игру каждый день недели и забирайте награду дня — лучшая ждёт на седьмой. Каждый понедельник начинается новый журнал.",
                    "سجّل الدخول كل يوم من أيام الأسبوع لتحصل على مكافأة اليوم؛ وأفضلها في اليوم السابع. يبدأ سجل جديد كل يوم اثنين.",
                    "毎日ログインしてその日の報酬を受け取ろう。7日目が一番豪華です。毎週月曜日に新しい日誌が始まります。",
                    "매일 접속해 그날의 보상을 받으세요. 7일째에 가장 좋은 보상이 있습니다. 매주 월요일에 새 일지가 시작됩니다.",
                    "本周每天登录即可领取当天奖励，第七天的奖励最丰厚。每周一开启新的日志。",
                    "本週每天登入即可領取當天獎勵，第七天的獎勵最豐厚。每週一開啟新的日誌。",
                    "เข้าเกมทุกวันในสัปดาห์เพื่อรับรางวัลประจำวัน รางวัลที่ดีที่สุดอยู่ในวันที่เจ็ด บันทึกใหม่เริ่มทุกวันจันทร์",
                    "Đăng nhập mỗi ngày trong tuần để nhận phần thưởng của ngày; phần tốt nhất ở ngày thứ bảy. Mỗi thứ Hai bắt đầu một nhật ký mới.",
                    "Masuk setiap hari dalam seminggu untuk mengambil hadiah hari itu; yang terbaik ada di hari ketujuh. Log baru dimulai setiap Senin.",
                    "Log masuk setiap hari dalam minggu untuk menuntut ganjaran hari itu; yang terbaik pada hari ketujuh. Log baharu bermula setiap Isnin."),
                Stages =
                {
                    Stage(1, Res("Titanium", 5000)),
                    Stage(2, Item("speed_any_5", 3)),
                    Stage(3, Res("Food", 10000)),
                    Stage(4, Item("chest_bronze", 1)),
                    Stage(5, Item("speed_any_60", 1)),
                    Stage(6, Res("Biomass", 5000)),
                    Stage(7, Res("Hadalite", 50), Item("chest_silver", 1))
                }
            }
        };

        // --- Helpers ------------------------------------------------------------------------------

        /// <summary>The event languages, in the panel's order.</summary>
        internal static readonly string[] Languages =
            { "tr", "en", "de", "fr", "es", "it", "pt", "nl", "ru", "ar", "ja", "ko", "zh-Hans", "zh-Hant", "th", "vi", "id", "ms" };

        private static Dictionary<string, string> T(params string[] texts)
        {
            if (texts.Length != Languages.Length)
                throw new ArgumentException($"Expected {Languages.Length} texts, got {texts.Length}.");
            return Languages.Zip(texts).ToDictionary(p => p.First, p => p.Second);
        }

        private static EventActionConfig A(string action, long points, long per = 1) => new() { Action = action, Points = points, Per = per };

        private static EventPhaseConfig Phase(double startHour, Dictionary<string, string> title, params EventActionConfig[] actions) =>
            new() { StartHour = startHour, Hours = 4, Title = title, Actions = actions.ToList() };

        private static EventRewardConfig Res(string id, long amount) => new() { Type = "resource", Id = id, Amount = amount };
        private static EventRewardConfig Item(string id, long amount) => new() { Type = "item", Id = id, Amount = amount };

        private static EventTierConfig Stage(long points, params EventRewardConfig[] rewards) => new() { Points = points, Rewards = rewards.ToList() };
        private static EventTierConfig Band(int from, int to, params EventRewardConfig[] rewards) => new() { From = from, To = to, Rewards = rewards.ToList() };
    }
}
