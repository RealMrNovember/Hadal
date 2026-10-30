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
        public const string Mobilization = "mobilization";
        public const string AllianceDuel = "alliance-duel";
        public const string TrenchConquest = "trench-conquest";

        /// <summary>Every default schedule's id (mails name their events by a localisation key "event.name.{id}").</summary>
        public static readonly string[] Ids = { DepthRace, AllianceWeek, LoginWeek, Mobilization, AllianceDuel, TrenchConquest };

        private static readonly int[] EveryDay = { 0, 1, 2, 3, 4, 5, 6 };
        private const int Monday = (int)DayOfWeek.Monday;
        private const int Thursday = (int)DayOfWeek.Thursday;

        /// <summary>Every default schedule: what a new, empty calendar is seeded with.</summary>
        public static List<EventSchedule> All(DateTimeOffset now) => new() { Depth(now), Alliance(now), Login(now), Mobilize(now), Duel(now), Trench(now) };

        /// <summary>
        /// Defaults that came after the first seed (roadmap §7, E6). A calendar seeded before them gets each once
        /// (EventService.Calendar, marker "seed:{id}"), without touching the schedules it has.
        /// </summary>
        public static List<EventSchedule> Added(DateTimeOffset now) => new() { Mobilize(now), Duel(now), Trench(now) };

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

        /// <summary>
        /// Mobilization (docs/29 §4.5): Thursday 00:00 UTC for three days. Each alliance of 15+ members gets a pool of 8
        /// task cards; members take one at a time. Personal point chests and an alliance ranking per kingdom.
        /// </summary>
        private static EventSchedule Mobilize(DateTimeOffset now) => new()
        {
            Id = Mobilization,
            Weekdays = new List<int> { Thursday },
            StartMinute = 0,
            DurationHours = 72,
            LeadHours = 24,
            Note = "Default: alliance task pool, Thursday to Saturday",
            CreatedBy = EventService.CalendarAuthor,
            CreatedAt = now,
            UpdatedAt = now,
            Blueprint = new EventDefinition
            {
                Template = EventTemplates.Mobilization,
                Banner = "trench",
                ClaimDays = 1,
                MinRankPoints = 300,
                Title = T("Seferberlik", "Mobilization", "Mobilmachung", "Mobilisation", "Movilización", "Mobilitazione", "Mobilização", "Mobilisatie",
                    "Мобилизация", "التعبئة", "総動員", "총동원", "总动员", "總動員", "การระดมพล", "Tổng động viên", "Mobilisasi", "Mobilisasi"),
                Description = T(
                    "İttifakının ortak bir görev havuzu var. Bir seferde bir görev al, oynayarak bitir; puanı hem sana hem ittifakına yazılır. Beğenmediğin görevi bırak ya da yenile. Katılmak için ittifakın en az 15 üyesi olmalı.",
                    "Your alliance shares a pool of tasks. Take one at a time and finish it by playing: its points count for you and for your alliance. Drop or refresh a task you do not like. An alliance needs at least 15 members to take part.",
                    "Deine Allianz teilt sich einen Aufgabenpool. Nimm jeweils eine Aufgabe und erledige sie beim Spielen: Ihre Punkte zählen für dich und deine Allianz. Wirf eine Aufgabe ab oder erneuere sie, wenn sie dir nicht gefällt. Eine Allianz braucht mindestens 15 Mitglieder.",
                    "Votre alliance partage une réserve de missions. Prenez-en une à la fois et accomplissez-la en jouant : ses points comptent pour vous et pour l'alliance. Abandonnez ou renouvelez une mission qui ne vous plaît pas. Une alliance doit compter au moins 15 membres.",
                    "Tu alianza comparte una reserva de tareas. Toma una cada vez y complétala jugando: sus puntos cuentan para ti y para tu alianza. Abandona o renueva una tarea que no te guste. La alianza necesita al menos 15 miembros.",
                    "La tua alleanza condivide una riserva di compiti. Prendine uno alla volta e completalo giocando: i punti valgono per te e per l'alleanza. Abbandona o rinnova un compito che non ti piace. Servono almeno 15 membri.",
                    "Sua aliança compartilha um conjunto de tarefas. Pegue uma por vez e conclua jogando: os pontos contam para você e para a aliança. Descarte ou renove uma tarefa de que não goste. A aliança precisa de pelo menos 15 membros.",
                    "Je alliantie deelt een pool met taken. Neem er één tegelijk en voltooi die door te spelen: de punten tellen voor jou en je alliantie. Laat een taak vallen of vernieuw hem als hij je niet bevalt. Een alliantie heeft minstens 15 leden nodig.",
                    "У вашего альянса общий набор заданий. Берите по одному и выполняйте в игре: очки идут вам и альянсу. Неподходящее задание можно сбросить или обновить. Для участия в альянсе должно быть не меньше 15 участников.",
                    "لدى تحالفك مجموعة مهام مشتركة. خذ مهمة واحدة في كل مرة وأنجزها أثناء اللعب: تُحتسب نقاطها لك ولتحالفك. يمكنك ترك المهمة أو تجديدها إن لم تعجبك. يحتاج التحالف إلى 15 عضوًا على الأقل للمشاركة.",
                    "同盟には共有のタスクプールがあります。一度に1つ受けてプレイで達成すると、ポイントが自分と同盟に入ります。気に入らないタスクは破棄または更新できます。参加には同盟メンバー15人以上が必要です。",
                    "연맹은 공동 임무 풀을 공유합니다. 한 번에 하나씩 받아 플레이로 완료하면 점수가 나와 연맹 모두에게 쌓입니다. 마음에 들지 않는 임무는 포기하거나 새로 고칠 수 있습니다. 참가하려면 연맹원이 15명 이상이어야 합니다.",
                    "你的联盟共享一个任务池。每次领取一个任务，通过游戏完成，积分同时计入你和联盟。不喜欢的任务可以放弃或刷新。联盟至少需要15名成员才能参加。",
                    "你的聯盟共享一個任務池。每次領取一個任務，透過遊戲完成，積分同時計入你和聯盟。不喜歡的任務可以放棄或刷新。聯盟至少需要15名成員才能參加。",
                    "พันธมิตรของคุณมีกองภารกิจร่วมกัน รับได้ครั้งละหนึ่งภารกิจและทำให้สำเร็จด้วยการเล่น แต้มจะนับให้ทั้งคุณและพันธมิตร ทิ้งหรือรีเฟรชภารกิจที่ไม่ชอบได้ พันธมิตรต้องมีสมาชิกอย่างน้อย 15 คน",
                    "Liên minh của bạn có chung một kho nhiệm vụ. Mỗi lần nhận một nhiệm vụ và hoàn thành khi chơi: điểm được tính cho bạn và liên minh. Có thể bỏ hoặc làm mới nhiệm vụ không ưng ý. Liên minh cần ít nhất 15 thành viên.",
                    "Aliansimu berbagi kumpulan tugas. Ambil satu per satu dan selesaikan sambil bermain: poinnya untukmu dan aliansimu. Buang atau segarkan tugas yang tidak kamu suka. Aliansi perlu minimal 15 anggota.",
                    "Pakatan anda berkongsi satu kumpulan tugasan. Ambil satu demi satu dan siapkan sambil bermain: mata dikira untuk anda dan pakatan. Buang atau segarkan tugasan yang tidak disukai. Pakatan perlu sekurang-kurangnya 15 ahli."),
                Mobilization = new MobilizationConfig(),
                Stages =
                {
                    Stage(400, Res("Titanium", 10000), Item("speed_any_5", 2)),
                    Stage(1200, Item("chest_bronze", 1), Res("Food", 10000)),
                    Stage(2500, Item("chest_silver", 1)),
                    Stage(5000, Item("chest_silver", 1), Res("Hadalite", 30))
                },
                RankBands =
                {
                    Band(1, 1, Res("Hadalite", 150), Item("chest_gold", 1)),
                    Band(2, 3, Res("Hadalite", 80), Item("chest_silver", 1)),
                    Band(4, 10, Res("Hadalite", 30), Item("chest_bronze", 2))
                }
            }
        };

        /// <summary>
        /// Alliance Duel (docs/29 §4.3): Monday 00:00 UTC, six themed days to Saturday 24:00. Alliances of a kingdom are
        /// paired by power when it starts; day wins 1/2/2/2/2/4, first to 7 wins. Daily personal chests; result mails.
        /// </summary>
        private static EventSchedule Duel(DateTimeOffset now) => new()
        {
            Id = AllianceDuel,
            Weekdays = new List<int> { Monday },
            StartMinute = 0,
            DurationHours = 6 * 24,
            LeadHours = 24,
            Note = "Default: weekly 1-v-1 alliance duel, Monday to Saturday",
            CreatedBy = EventService.CalendarAuthor,
            CreatedAt = now,
            UpdatedAt = now,
            Blueprint = new EventDefinition
            {
                Template = EventTemplates.AllianceDuel,
                Banner = "leviathan",
                ClaimDays = 1,
                MinRankPoints = 500,
                Title = T("İttifak Düellosu", "Alliance Duel", "Allianzduell", "Duel d'alliances", "Duelo de alianzas", "Duello tra alleanze",
                    "Duelo de alianças", "Alliantieduel", "Дуэль альянсов", "مبارزة التحالفات", "同盟デュエル", "연맹 결투", "联盟对决", "聯盟對決",
                    "ศึกดวลพันธมิตร", "Đấu tay đôi liên minh", "Duel Aliansi", "Duel Pakatan"),
                Description = T(
                    "Her hafta ittifakın, krallığındaki güce yakın bir ittifakla karşılaşır. Altı temalı gün var; günü kazanmak sırasıyla 1, 2, 2, 2, 2 ve 4 puan getirir, 7'ye ilk ulaşan haftayı kazanır. Her gün kendi puanınla günlük sandıkları açarsın.",
                    "Every week your alliance meets an alliance of similar power in your kingdom. There are six themed days; winning a day is worth 1, 2, 2, 2, 2 and 4 points, and the first to 7 wins the week. Your own points each day open daily chests.",
                    "Jede Woche trifft deine Allianz auf eine ähnlich starke Allianz deines Königreichs. Es gibt sechs Thementage; ein gewonnener Tag bringt 1, 2, 2, 2, 2 und 4 Punkte, wer zuerst 7 hat, gewinnt die Woche. Deine eigenen Tagespunkte öffnen Tagestruhen.",
                    "Chaque semaine, votre alliance affronte une alliance de puissance proche dans votre royaume. Six journées à thème : gagner une journée rapporte 1, 2, 2, 2, 2 puis 4 points, et la première à 7 remporte la semaine. Vos propres points du jour ouvrent des coffres quotidiens.",
                    "Cada semana tu alianza se enfrenta a una alianza de poder similar en tu reino. Hay seis días temáticos; ganar un día vale 1, 2, 2, 2, 2 y 4 puntos, y la primera en llegar a 7 gana la semana. Tus propios puntos de cada día abren cofres diarios.",
                    "Ogni settimana la tua alleanza sfida un'alleanza di potere simile nel tuo regno. Sei giornate a tema: vincere una giornata vale 1, 2, 2, 2, 2 e 4 punti, e chi arriva prima a 7 vince la settimana. I tuoi punti del giorno aprono i forzieri giornalieri.",
                    "Toda semana sua aliança enfrenta uma aliança de poder parecido no seu reino. São seis dias temáticos; vencer um dia vale 1, 2, 2, 2, 2 e 4 pontos, e quem chegar primeiro a 7 vence a semana. Seus próprios pontos de cada dia abrem baús diários.",
                    "Elke week neemt je alliantie het op tegen een even sterke alliantie in je koninkrijk. Zes themadagen; een dag winnen levert 1, 2, 2, 2, 2 en 4 punten op, en wie eerst 7 heeft wint de week. Je eigen dagpunten openen dagkisten.",
                    "Каждую неделю ваш альянс встречается с альянсом схожей силы из вашего королевства. Шесть тематических дней: победа в дне приносит 1, 2, 2, 2, 2 и 4 очка, кто первым наберёт 7 — выигрывает неделю. Ваши личные очки за день открывают ежедневные сундуки.",
                    "كل أسبوع يواجه تحالفك تحالفًا بقوة مقاربة في مملكتك. هناك ستة أيام ذات مواضيع؛ الفوز بيوم يمنح 1 و2 و2 و2 و2 و4 نقاط، وأول من يصل إلى 7 يفوز بالأسبوع. نقاطك الخاصة في كل يوم تفتح صناديق يومية.",
                    "毎週、同盟は王国内の近い戦力の同盟と対戦します。テーマのある6日間で、勝った日はそれぞれ1・2・2・2・2・4ポイント。先に7ポイントに達した側が週の勝者です。毎日の自分のポイントでデイリー宝箱が開きます。",
                    "매주 연맹은 같은 왕국에서 전투력이 비슷한 연맹과 맞붙습니다. 테마가 있는 6일 동안 하루를 이기면 1, 2, 2, 2, 2, 4점을 얻고 먼저 7점에 도달한 쪽이 주간 승리합니다. 매일 내 점수로 일일 상자를 엽니다.",
                    "每周你的联盟会与本王国实力相近的联盟对决。共六个主题日，赢下一天分别得1、2、2、2、2、4分，先到7分者赢得本周。你每天的个人积分可开启每日宝箱。",
                    "每週你的聯盟會與本王國實力相近的聯盟對決。共六個主題日，贏下一天分別得1、2、2、2、2、4分，先到7分者贏得本週。你每天的個人積分可開啟每日寶箱。",
                    "ทุกสัปดาห์พันธมิตรของคุณจะพบกับพันธมิตรที่มีพลังใกล้เคียงกันในอาณาจักร มีหกวันตามธีม ชนะแต่ละวันได้ 1, 2, 2, 2, 2 และ 4 แต้ม ใครถึง 7 ก่อนชนะสัปดาห์นั้น แต้มของคุณเองในแต่ละวันเปิดหีบรายวันได้",
                    "Mỗi tuần liên minh của bạn đối đầu một liên minh có sức mạnh tương đương trong vương quốc. Có sáu ngày theo chủ đề; thắng một ngày được 1, 2, 2, 2, 2 và 4 điểm, ai đạt 7 trước thắng cả tuần. Điểm riêng mỗi ngày của bạn mở rương hằng ngày.",
                    "Setiap minggu aliansimu bertemu aliansi dengan kekuatan serupa di kerajaanmu. Ada enam hari bertema; menang satu hari bernilai 1, 2, 2, 2, 2, dan 4 poin, dan yang pertama mencapai 7 memenangkan minggu itu. Poinmu sendiri tiap hari membuka peti harian.",
                    "Setiap minggu pakatan anda berdepan pakatan yang hampir sama kuat dalam kerajaan anda. Ada enam hari bertema; menang satu hari bernilai 1, 2, 2, 2, 2 dan 4 mata, dan yang pertama mencapai 7 memenangi minggu itu. Mata anda sendiri setiap hari membuka peti harian."),
                Phases =
                {
                    Day(0, T("Toplama Günü", "Gathering Day", "Sammeltag", "Journée de récolte", "Día de recolección", "Giornata di raccolta",
                            "Dia de coleta", "Verzameldag", "День добычи", "يوم الجمع", "採集の日", "채집의 날", "采集日", "採集日",
                            "วันเก็บทรัพยากร", "Ngày thu thập", "Hari Pengumpulan", "Hari Mengumpul"),
                        A(EventActions.Gather, 1, 1000)),
                    Day(1, T("Üs Büyütme", "Base Growth", "Basisausbau", "Croissance de la base", "Crecimiento de la base", "Crescita della base",
                            "Crescimento da base", "Basisgroei", "Рост базы", "نمو القاعدة", "基地発展", "기지 성장", "基地发展", "基地發展",
                            "พัฒนาฐาน", "Phát triển căn cứ", "Pertumbuhan Markas", "Pertumbuhan Pangkalan"),
                        A(EventActions.BuildPower, 2), A(EventActions.SpeedupMinutes, 1)),
                    Day(2, T("Araştırma", "Research", "Forschung", "Recherche", "Investigación", "Ricerca",
                            "Pesquisa", "Onderzoek", "Исследования", "البحث", "研究", "연구", "研究", "研究",
                            "วิจัย", "Nghiên cứu", "Riset", "Penyelidikan"),
                        A(EventActions.Research, 300), A(EventActions.SpeedupMinutes, 1)),
                    Day(3, T("Kahramanlar", "Heroes", "Helden", "Héros", "Héroes", "Eroi",
                            "Heróis", "Helden", "Герои", "الأبطال", "英雄", "영웅", "英雄", "英雄",
                            "ฮีโร่", "Anh hùng", "Pahlawan", "Wira"),
                        A(EventActions.HeroLevel, 50), A(EventActions.HeroPull, 200), A(EventActions.HeroRecruit, 300)),
                    Day(4, T("Topyekûn Seferberlik", "Total Mobilisation", "Totale Mobilmachung", "Mobilisation générale", "Movilización total", "Mobilitazione totale",
                            "Mobilização total", "Totale mobilisatie", "Всеобщая мобилизация", "التعبئة الشاملة", "総力戦", "총력전", "全面动员", "全面動員",
                            "ระดมพลเต็มกำลัง", "Tổng lực", "Mobilisasi Total", "Mobilisasi Penuh"),
                        A(EventActions.Train(1), 1), A(EventActions.Train(2), 2), A(EventActions.Train(3), 4), A(EventActions.Train(4), 8), A(EventActions.Train(5), 16),
                        A(EventActions.Gather, 1, 2000), A(EventActions.BuildPower, 1), A(EventActions.Research, 150), A(EventActions.Heal, 1, 10)),
                    Day(5, T("Av", "The Hunt", "Die Jagd", "La chasse", "La caza", "La caccia",
                            "A caçada", "De jacht", "Охота", "الصيد", "狩り", "사냥", "狩猎", "狩獵",
                            "การล่า", "Cuộc săn", "Perburuan", "Perburuan"),
                        A(EventActions.MonsterKill, 100), A(EventActions.MonsterLevels, 20))
                },
                Stages =
                {
                    Stage(1000, Res("Titanium", 5000), Item("speed_any_5", 1)),
                    Stage(4000, Item("chest_bronze", 1)),
                    Stage(10000, Item("chest_silver", 1))
                },
                Duel = new DuelConfig
                {
                    Win = { Res("Hadalite", 100), Item("chest_gold", 1) },
                    Loss = { Item("chest_silver", 1) },
                    Draw = { Res("Hadalite", 50), Item("chest_silver", 1) }
                }
            }
        };

        private static EventPhaseConfig Day(int day, Dictionary<string, string> title, params EventActionConfig[] actions) =>
            new() { StartHour = day * 24, Hours = 24, Title = title, Actions = actions.ToList() };

        // --- Helpers ------------------------------------------------------------------------------

        /// <summary>The event languages, in the panel's order.</summary>
        internal static readonly string[] Languages =
            { "tr", "en", "de", "fr", "es", "it", "pt", "nl", "ru", "ar", "ja", "ko", "zh-Hans", "zh-Hant", "th", "vi", "id", "ms" };

        /// <summary>
        /// Trench Conquest (docs/29 §4.6): Monday 00:00 UTC for a week. Registration the first two days, the draw on
        /// Wednesday 00:00, rounds at 12:00 UTC Wednesday to Sunday. Personal points per fight won (stages), alliance rank
        /// rewards by league points to the registered members.
        /// </summary>
        private static EventSchedule Trench(DateTimeOffset now) => new()
        {
            Id = TrenchConquest,
            Weekdays = new List<int> { Monday },
            StartMinute = 0,
            DurationHours = 7 * 24,
            LeadHours = 24,
            Note = "Default: weekly Trench Conquest league (3 lanes, groups of six, five rounds)",
            CreatedBy = EventService.CalendarAuthor,
            CreatedAt = now,
            UpdatedAt = now,
            Blueprint = new EventDefinition
            {
                Template = EventTemplates.TrenchConquest,
                Banner = "abyss",
                ClaimDays = 1,
                MinRankPoints = 1,
                Trench = new TrenchConfig(),
                Title = T("Hendek Fethi", "Trench Conquest", "Grabeneroberung", "Conquête des tranchées", "Conquista de fosas", "Conquista delle fosse",
                    "Conquista das fossas", "Troggenverovering", "Захват впадин", "غزو الخنادق", "海溝制圧", "해구 정복", "海沟征服", "海溝征服",
                    "พิชิตร่องลึก", "Chinh phục rãnh sâu", "Penaklukan Palung", "Penaklukan Palung"),
                Description = T(
                    "İlk iki gün takımını üç hattan birine kaydet (reaktör 10+). Yeterli takımı olan ittifaklar altılı gruplara ayrılır ve beş tur karşılaşır. Her hatta takımlar en zayıftan başlayarak sırayla dövüşür; daha çok bayrak alan turu kazanır. Askerler ölmez.",
                    "Register your team into one of three lanes in the first two days (reactor 10+). Alliances with enough teams are grouped six by six and meet over five rounds. In each lane teams fight in turn, weakest first; more flags wins the round. Troops never die.",
                    "Melde dein Team in den ersten zwei Tagen in einer von drei Bahnen an (Reaktor 10+). Allianzen mit genug Teams werden in Sechsergruppen eingeteilt und treffen in fünf Runden aufeinander. In jeder Bahn kämpfen die Teams nacheinander, das schwächste zuerst; mehr Flaggen gewinnen die Runde. Truppen sterben nie.",
                    "Inscrivez votre équipe dans l'un des trois couloirs les deux premiers jours (réacteur 10+). Les alliances avec assez d'équipes sont réparties en groupes de six et s'affrontent en cinq manches. Dans chaque couloir, les équipes combattent à tour de rôle, la plus faible d'abord ; le plus de drapeaux gagne la manche. Les troupes ne meurent jamais.",
                    "Inscribe tu equipo en uno de los tres carriles los dos primeros días (reactor 10+). Las alianzas con suficientes equipos se agrupan de seis en seis y se enfrentan en cinco rondas. En cada carril los equipos luchan por turnos, el más débil primero; más banderas gana la ronda. Las tropas nunca mueren.",
                    "Iscrivi la tua squadra in una delle tre corsie nei primi due giorni (reattore 10+). Le alleanze con abbastanza squadre sono divise in gironi da sei e si affrontano in cinque turni. In ogni corsia le squadre combattono a turno, la più debole per prima; più bandiere vincono il turno. Le truppe non muoiono mai.",
                    "Inscreva sua equipe em uma das três faixas nos dois primeiros dias (reator 10+). Alianças com equipes suficientes são agrupadas de seis em seis e se enfrentam em cinco rodadas. Em cada faixa as equipes lutam em sequência, a mais fraca primeiro; mais bandeiras vencem a rodada. As tropas nunca morrem.",
                    "Schrijf je team de eerste twee dagen in voor een van drie banen (reactor 10+). Allianties met genoeg teams worden in groepen van zes verdeeld en spelen vijf rondes. Per baan vechten teams om de beurt, de zwakste eerst; meer vlaggen wint de ronde. Troepen sterven nooit.",
                    "В первые два дня запишите отряд на одну из трёх линий (реактор 10+). Альянсы с достаточным числом отрядов делятся на группы по шесть и встречаются в пяти раундах. На каждой линии отряды бьются по очереди, начиная со слабейшего; больше флагов — победа в раунде. Войска не гибнут.",
                    "سجّل فريقك في أحد الممرات الثلاثة خلال أول يومين (المفاعل 10+). تُقسَّم التحالفات التي لديها فرق كافية إلى مجموعات من ستة وتتواجه في خمس جولات. في كل ممر تتقاتل الفرق بالتناوب بدءًا بالأضعف؛ صاحب الأعلام الأكثر يفوز بالجولة. لا تموت القوات.",
                    "最初の2日間に3つのレーンのいずれかへ部隊を登録します（リアクター10以上）。十分な部隊数の同盟は6つずつのグループに分かれ、5ラウンド戦います。各レーンでは弱い部隊から順に戦い、旗を多く取った側がラウンドの勝者です。兵は死にません。",
                    "처음 이틀 동안 세 개의 라인 중 하나에 부대를 등록하세요(원자로 10 이상). 부대가 충분한 연맹은 여섯씩 조를 이뤄 다섯 라운드를 치릅니다. 각 라인에서 부대는 약한 순서대로 차례로 싸우고, 깃발을 더 많이 얻은 쪽이 라운드에서 이깁니다. 병력은 죽지 않습니다.",
                    "前两天将你的部队登记到三条战线之一（反应堆10级以上）。部队足够的联盟按六个一组分组，进行五轮对决。每条战线上部队从最弱的开始依次作战，夺旗多者赢得本轮。士兵不会阵亡。",
                    "前兩天將你的部隊登記到三條戰線之一（反應爐10級以上）。部隊足夠的聯盟按六個一組分組，進行五輪對決。每條戰線上部隊從最弱的開始依次作戰，奪旗多者贏得本輪。士兵不會陣亡。",
                    "ลงทะเบียนทีมของคุณในหนึ่งในสามเลนภายในสองวันแรก (เตาปฏิกรณ์ 10+) พันธมิตรที่มีทีมเพียงพอจะถูกแบ่งกลุ่มละหกและพบกันห้ารอบ ในแต่ละเลนทีมจะสู้กันตามลำดับโดยเริ่มจากทีมที่อ่อนที่สุด ได้ธงมากกว่าชนะรอบนั้น ทหารไม่ตาย",
                    "Đăng ký đội của bạn vào một trong ba làn trong hai ngày đầu (lò phản ứng 10+). Các liên minh có đủ đội được chia nhóm sáu và đấu năm vòng. Ở mỗi làn các đội lần lượt chiến đấu, yếu nhất trước; nhiều cờ hơn thắng vòng. Quân không bao giờ chết.",
                    "Daftarkan timmu ke salah satu dari tiga jalur dalam dua hari pertama (reaktor 10+). Aliansi dengan tim yang cukup dibagi dalam grup berisi enam dan bertemu dalam lima ronde. Di tiap jalur tim bertarung bergiliran, mulai dari yang terlemah; bendera terbanyak memenangkan ronde. Pasukan tidak pernah mati.",
                    "Daftarkan pasukan anda ke salah satu daripada tiga lorong dalam dua hari pertama (reaktor 10+). Pakatan yang cukup pasukan dibahagi kepada kumpulan enam dan bertemu dalam lima pusingan. Di setiap lorong pasukan berlawan bergilir, bermula dengan yang paling lemah; lebih banyak bendera menang pusingan. Tentera tidak pernah mati."),
                Stages =
                {
                    Stage(100, Res("Titanium", 10_000), Item("speed_any_5", 2)),
                    Stage(300, Item("chest_bronze", 1)),
                    Stage(600, Item("chest_silver", 1))
                },
                RankBands =
                {
                    Band(1, 1, Res("Hadalite", 150), Item("chest_gold", 1)),
                    Band(2, 3, Res("Hadalite", 80), Item("chest_silver", 1)),
                    Band(4, 10, Res("Hadalite", 30), Item("chest_bronze", 2))
                }
            }
        };

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
