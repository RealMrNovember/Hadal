# Sözlük

Kılavuzda geçen terimler, oyuncuya görünen Türkçe adları ve koddaki kimlikleri. Kimlikler İngilizce
`snake_case` kalır; oyuncuya görünen her metin Türkçedir (K-18).

## Genel terimler

| Terim | Anlamı |
|---|---|
| Sv | Seviye |
| R | Çekirdek Reaktör seviyesi (ör. R30) |
| F2P | Hiç ödeme yapmayan oyuncu |
| Ham süre | Hız bonusları, yardım ve hızlandırma uygulanmadan önceki süre |
| Etkin süre | ham / (1 + Σhız%) − yardım − ücretsiz bitirme (K-13) |
| Kritik yol | Reaktör 50'ye giden zorunlu inşaat zinciri: reaktör + önkoşulları |
| Önkoşul zinciri | Reaktörün her seviyesinin istediği binalar |
| Eşdeğer seviye | Kubbelerde bir seviyenin bağlı olduğu reaktör kapısı |
| Evrim katmanı | Binanın her 10 seviyede değişen görünümü |
| Güç | Binalar + araştırma + askerler + kahramanlar toplamı |
| Tavan | Bir kaynaktan en fazla tutulabilecek miktar |
| Kaynak Kapsülü | Tavana takılmayan, çantaya düşen ödül kaynağı |
| Takviye (Boost) | Bir üretim binasına 30 dakika %30 ek üretim |
| Aşırı Yük (Overdrive) | Reaktöre 1 saat %50 ek enerji, 8 saatte bir |
| Kurtarma görevi | Dron Fabrikası'nın 15 dk / 1 sa / 4 sa görevleri |
| Komuta ekibi | Ekonomi becerileri çalışan 3 kahraman yuvası |
| Kaptan / Subay | Denizaltıdaki savaş kahramanları (1 kaptan + 2 subay) |
| Basınç sınıfı | Gövdenin dayanabildiği derinlik, P1–P10 |
| Kuşak | Dünya haritasının derinlik halkası |
| Basınç kilidi | Kuşaklar arasındaki, ittifakların ele geçirdiği geçit |
| Sonar Enerjisi | Haritadaki PvE saldırılarının harcadığı puan |
| Derinlik Kroniği | Sunucunun ortak zaman çizelgesi |
| Köken | Oyuncunun başta seçtiği fraksiyon |
| Egemen | Çukur Kalbi'ni tutan ittifakın lideri |
| Akıntı Savaşı | Çukurlar (sunucular) arası sezon savaşı |
| Tür Kataloğu | Sonarla taranan gerçek canlıların koleksiyonu |
| Kadim Şema | Bazı binaların 49 → 50 adımı için gereken kalıntı |
| Basınç Kodeksi | Savaş Kubbesi'nin 4. ve 5. seviyesi için gereken kalıntı |
| Anı Kristali | Kahraman yetenek seviyesi malzemesi |
| Kurtarma Kapsülü | Sezon sonunda ölü askerlerin %50'sini geri veren ödül |
| The Syndicate | Mağaza (bu adla anılır, K-06) |
| Sendika Sözleşmesi | Aylık kart |
| Derin Akıntı Bileti | Sezon bileti |
| Rezonans Alanı | Haritada sahibine buf veren baca, sızıntı, nodül ve kalıntı alanları |
| Gelgit Penceresi | Geçit ve alan saldırılarının yapılabildiği günlük 3 saatlik pencere |
| Fay Geçidi | Haritadaki kanatları ayıran geçit |
| Sessizlik Kalkanı | Saldırıya karşı koruma kalkanı |
| Çukur Pasaportu | Sunucular arası göç belgesi |
| Derinlik İzni | VIP sisteminin adı |

## Kaynaklar

| Türkçe ad | Kimlik | Sembol |
|---|---|---|
| Oksijen | `Oxygen` | O₂ |
| Enerji | `Energy` | EN |
| Gıda | `Food` | GD |
| Biyokütle | `Biomass` | BK |
| Titanyum | `Titanium` | Ti |
| Süperalaşım | `Alloy` | SA |
| Hadalit | `Hadalite` | HD |

## Nadirlikler, sınıflar, fraksiyonlar

| Türkçe ad | Kimlik |
|---|---|
| Sıradan · Nadir · Destansı · Efsanevi | `common` · `rare` · `epic` · `legendary` |
| Komutan · Bilim insanı · Mühendis · Avcı · Hekim · Pilot | `Commander` · `Scientist` · `Engineer` · `Hunter` · `Medic` · `Pilot` |
| Atlantik Birliği · Pasifik Tarikatı · Derindoğanlar · Unutulanlar · Uçurum Tarikatı | `AtlanticUnion` · `PacificOrder` · `Deepborn` · `ForgottenOnes` · `AbyssCult` |

## Asker türleri

| Türkçe ad | Kimlik | Not |
|---|---|---|
| Zırhlı Dalgıç | `armored` | Ark Askerine karşı üstün |
| Zıpkıncı | `harpoon` | Zırhlı Dalgıça karşı üstün |
| Ark Askeri | `arc` | Zıpkıncıya karşı üstün |
| Delici | `driller` | Kuşatma; binalara karşı güçlü, en yüksek taşıma |

## Binalar

<!-- URET:sozluk-binalar -->
| Türkçe ad | Kimlik | Kategori |
| --- | --- | --- |
| [Çekirdek Reaktör](./binalar/core_reactor.md) | `core_reactor` | Merkez |
| [Oksijen Tesisi](./binalar/oxygen_plant.md) | `oxygen_plant` | Üretim |
| [Gıda Laboratuvarı](./binalar/food_lab.md) | `food_lab` | Üretim |
| [Biyo Laboratuvar](./binalar/bio_lab.md) | `bio_lab` | Üretim |
| [Hidroponik Çiftlik](./binalar/hydroponic_farm.md) | `hydroponic_farm` | Üretim |
| [Dron Fabrikası](./binalar/drone_factory.md) | `drone_factory` | Üretim |
| [Araştırma Merkezi](./binalar/research_center.md) | `research_center` | Araştırma |
| [Denizaltı İskelesi](./binalar/submarine_dock.md) | `submarine_dock` | Askeri |
| [Radar Merkezi](./binalar/radar_center.md) | `radar_center` | Askeri |
| [Askeri Kubbe](./binalar/military_dome.md) | `military_dome` | Askeri |
| [Kalkan Jeneratörü](./binalar/shield_generator.md) | `shield_generator` | Askeri |
| [Mega Kubbe](./binalar/mega_dome.md) | `mega_dome` | Kubbe |
| [Tarım Kubbesi](./binalar/agriculture_dome.md) | `agriculture_dome` | Kubbe |
| [İttifak Merkezi](./binalar/alliance_hub.md) | `alliance_hub` | İttifak |
| [Süs Kulesi](./binalar/decoration_spire.md) | `decoration_spire` | Süs |
| [Kriyo Salonu](./binalar/cryo_hall.md) | `cryo_hall` | Destek |
| [Basınç Ambarı](./binalar/pressure_vault.md) | `pressure_vault` | Depolama |
| [Mühendislik Bölmesi](./binalar/engineering_bay.md) | `engineering_bay` | Destek |
| [Denizaltı Hangarı](./binalar/submarine_hangar.md) | `submarine_hangar` | Askeri |
| [Tatbikat Havuzu](./binalar/training_grounds.md) | `training_grounds` | Askeri |
| [Mürettebat Yatakhanesi](./binalar/crew_quarters.md) | `crew_quarters` | Destek |
| [Revir](./binalar/med_bay.md) | `med_bay` | Askeri |
| [Kriyo Bakım Ünitesi](./binalar/cryo_ward.md) | `cryo_ward` | Askeri |
| [Hidrotermal Santral](./binalar/hydrothermal_plant.md) | `hydrothermal_plant` | Üretim |
| [Takas İstasyonu](./binalar/trade_station.md) | `trade_station` | Destek |
| [Bilim Kubbesi](./binalar/science_dome.md) | `science_dome` | Kubbe |
| [Savaş Kubbesi](./binalar/war_dome.md) | `war_dome` | Kubbe |
| [Teçhizat Atölyesi](./binalar/gear_workshop.md) | `gear_workshop` | Destek |
| [Dalgıç Anıtı](./binalar/armor_monument.md) | `armor_monument` | Anıt |
| [Zıpkın Anıtı](./binalar/harpoon_monument.md) | `harpoon_monument` | Anıt |
| [Ark Anıtı](./binalar/arc_monument.md) | `arc_monument` | Anıt |
| [Kalıntı Arşivi](./binalar/relic_archive.md) | `relic_archive` | Destek |
| [Alaşım Dökümhanesi](./binalar/alloy_foundry.md) | `alloy_foundry` | Üretim |
| [Kronik Feneri](./binalar/chronicle_beacon.md) | `chronicle_beacon` | Destek |
<!-- /URET:sozluk-binalar -->

## Kahramanlar

<!-- URET:sozluk-kahramanlar -->
| Türkçe ad | Kimlik |
| --- | --- |
| [Komutan Elias Rourke](kahramanlar/mara.md) | `mara` |
| [Dr. Lyra Moss](kahramanlar/lyra.md) | `lyra` |
| [Tess Ruiz](kahramanlar/tess.md) | `tess` |
| [Vera Kostina](kahramanlar/vera.md) | `vera` |
| [Kael](kahramanlar/kael.md) | `kael` |
| [Dr. Orin Hale](kahramanlar/orin.md) | `orin` |
| [Kaptan Amara Okafor](kahramanlar/amara.md) | `amara` |
| [Rafael Quispe](kahramanlar/rafa.md) | `rafa` |
| [Darius Farahani](kahramanlar/darius.md) | `darius` |
| [Noa](kahramanlar/noa.md) | `noa` |
| [Juno Vega](kahramanlar/juno.md) | `juno` |
| [Sable](kahramanlar/sable.md) | `sable` |
| [Dr. Emi Sakamoto](kahramanlar/emi.md) | `emi` |
| [Ilo](kahramanlar/ilo.md) | `ilo` |
| [Mattias Gröndahl](kahramanlar/mattias.md) | `mattias` |
| [Deniz Karaca](kahramanlar/deniz.md) | `deniz` |
| [Dr. Priya Raman](kahramanlar/priya.md) | `priya` |
| [Odalys Ferreira](kahramanlar/odalys.md) | `odalys` |
| [Wira Santoso](kahramanlar/wira.md) | `wira` |
| [Aelis](kahramanlar/aelis.md) | `aelis` |
| [Amiral Kwame Boateng](kahramanlar/kwame.md) | `kwame` |
| [Meridyen](kahramanlar/meridyen.md) | `meridyen` |
| [Ufuk](kahramanlar/ufuk.md) | `ufuk` |
| [Saoirse Kavanagh](kahramanlar/saoirse.md) | `saoirse` |
| [Dr. Han Seo-yeon](kahramanlar/seoyeon.md) | `seoyeon` |
| [Kiri](kahramanlar/kiri.md) | `kiri` |
| [Sura](kahramanlar/sura.md) | `sura` |
| [Ilian Varga](kahramanlar/ilian.md) | `ilian` |
| [Dr. Isagani Dela Cruz](kahramanlar/isagani.md) | `isagani` |
| [Sela](kahramanlar/sela.md) | `sela` |
<!-- /URET:sozluk-kahramanlar -->

## Araştırmalar

<!-- URET:sozluk-arastirma -->
| Türkçe ad | Kimlik | Dal |
| --- | --- | --- |
| İnşaat Ustalığı | `construction_mastery` | Gelişim |
| Araştırma Yöntemleri | `research_methods` | Gelişim |
| İkinci İnşaatçı | `second_builder` | Gelişim |
| İkinci Tezgâh | `second_workbench` | Gelişim |
| Takviye Ayarı | `boost_tuning` | Gelişim |
| Kurtarma Planlaması | `salvage_planning` | Gelişim |
| Hiperbarik Kaynak | `hyperbaric_welding` | Gelişim |
| Aşırı Yük Soğutması | `overdrive_cooling` | Gelişim |
| Malzeme Verimliliği | `material_efficiency` | Gelişim |
| Numune Ekonomisi | `sample_economy` | Gelişim |
| Vardiya Düzeni | `crew_rotation` | Gelişim |
| Modüler Montaj | `modular_assembly` | Gelişim |
| Yağ Dengeli Elektronik | `oil_compensated_electronics` | Gelişim |
| Oksijen Verimi | `oxygen_yield` | Ekonomi |
| Gıda Verimi | `food_yield` | Ekonomi |
| Biyokütle Verimi | `biomass_yield` | Ekonomi |
| Titanyum Geri Kazanımı | `titanium_recovery` | Ekonomi |
| Depo Mühendisliği | `storage_engineering` | Ekonomi |
| Ek Oksijen Hattı | `extra_oxygen_line` | Ekonomi |
| Ek Dron Hattı | `extra_drone_line` | Ekonomi |
| Hidrotermal Isı Eşanjörü | `hydrothermal_exchange` | Ekonomi |
| Hidrolik Toplama Kolları | `gathering_arms` | Ekonomi |
| Hafif Yük Kapları | `cargo_bays` | Ekonomi |
| Deniz Karı Tuzakları | `marine_snow_traps` | Ekonomi |
| Kobalt Kabuğu Kazıma | `ferromanganese_crusts` | Ekonomi |
| Bakteri Matı Hasadı | `microbial_mat_harvest` | Ekonomi |
| Kemosentez Kültürü | `chemosynthesis_culture` | Ekonomi |
| Karanlık Oksijen | `dark_oxygen` | Ekonomi |
| Metan Hidrat Hücreleri | `methane_hydrate_cells` | Ekonomi |
| Hava Geri Dönüşümü | `air_recycling` | Hayatta Kalma |
| Kimyasal Oksijen Mumu | `oxygen_candles` | Hayatta Kalma |
| Basınç Gövdesi | `pressure_hull` | Hayatta Kalma |
| Triyaj Protokolü | `triage_protocol` | Hayatta Kalma |
| Revir Genişletme | `infirmary_expansion` | Hayatta Kalma |
| Erzak Disiplini | `field_rations` | Hayatta Kalma |
| Aerojel Yalıtım | `aerogel_insulation` | Hayatta Kalma |
| Sismik Ağ | `seismic_network` | Hayatta Kalma |
| Hiperbarik Tıp | `hyperbaric_medicine` | Hayatta Kalma |
| Hipoksi Tatbikatı | `hypoxia_drills` | Hayatta Kalma |
| Kriyo Taşma Protokolü | `cryo_protocol` | Hayatta Kalma |
| Tahliye Kapsülleri | `evacuation_pods` | Hayatta Kalma |
| Çok Huzmeli Sonar | `multibeam_sonar` | Derinlik |
| Balina Leşi Haritalama | `whale_fall_mapping` | Derinlik |
| Sintaktik Köpük | `syntactic_foam` | Derinlik |
| Sefer Lojistiği | `expedition_logistics` | Derinlik |
| SOFAR Kanalı Rölesi | `sofar_relay` | Derinlik |
| Tür Kataloğu Taraması | `species_survey` | Derinlik |
| Titanyum Basınç Küresi | `hull_titanium_sphere` | Derinlik |
| Sonar Enerjisi Kapasitörü | `sonar_capacitor` | Derinlik |
| Piyezolit Çalışması | `piezolyte_study` | Derinlik |
| Alümina Seramik Muhafaza | `hull_ceramic_housing` | Derinlik |
| Nodül Sahası Etüdü | `nodule_field_survey` | Derinlik |
| Süperalaşım Takviye Halkaları | `hull_superalloy_rings` | Derinlik |
| Kademe IV Doktrini | `tier_4_doctrine` | Birlik |
| Kademe V Doktrini | `tier_5_doctrine` | Birlik |
| Kademe VI Doktrini | `tier_6_doctrine` | Birlik |
| Kademe VII Doktrini | `tier_7_doctrine` | Birlik |
| Kademe VIII Doktrini | `tier_8_doctrine` | Birlik |
| Kademe IX Doktrini | `tier_9_doctrine` | Birlik |
| Kademe X Doktrini | `tier_10_doctrine` | Birlik |
| Hidrolik Pençe | `armored_attack` | Birlik |
| Demir Sülfür Plakalar | `armored_defense` | Birlik |
| Atmosferik Kabuk | `armored_health` | Birlik |
| Gaz Basınçlı Zıpkın | `harpoon_attack` | Birlik |
| Aramid Ağ Kalkanı | `harpoon_defense` | Birlik |
| Basınç Dengeli Solunum | `harpoon_health` | Birlik |
| Yüksek Gerilim Bobini | `arc_attack` | Birlik |
| Faraday Örgüsü | `arc_defense` | Birlik |
| Yalıtkan Jel Tulumu | `arc_health` | Birlik |
| Elmas Uçlu Matkap | `driller_attack` | Birlik |
| Titanyum Kafes Gövde | `driller_defense` | Birlik |
| Hidrolik Sönümleyici | `driller_health` | Birlik |
| Basınçlı Tatbikat | `pressure_drills` | Birlik |
| Parti Planlaması | `batch_planning` | Birlik |
| Komuta Simülatörü | `command_simulator` | Kahraman |
| Eğitim Bütçesi | `hero_training_budget` | Kahraman |
| Nöral Taktik Arayüzü | `neural_tactics` | Kahraman |
| Kişisel Basınç Zırhı | `personal_pressure_armor` | Kahraman |
| Biyomedikal İzleme | `biomedical_monitoring` | Kahraman |
| Mürettebat Koçluğu | `crew_coaching` | Kahraman |
| Subay Deneyimi | `officer_mentoring` | Kahraman |
| Adrenalin Protokolü | `adrenaline_protocol` | Kahraman |
| Teçhizat Uyumu | `gear_affinity` | Kahraman |
| Fraksiyon Sinerjisi | `faction_synergy` | Kahraman |
| Hücum Doktrini | `assault_doctrine` | Saldırı Taktikleri |
| Yağma Bölmeleri | `plunder_bays` | Saldırı Taktikleri |
| Hızlı Dönüş | `swift_return` | Saldırı Taktikleri |
| Basınçlı Hücum Kapsülleri | `assault_capsules` | Saldırı Taktikleri |
| Sessiz Pervane | `quiet_propellers` | Saldırı Taktikleri |
| Kalkanlı Yaklaşma | `shielded_approach` | Saldırı Taktikleri |
| Kalkan Delme | `shield_breach` | Saldırı Taktikleri |
| Toplu Hücum | `massed_assault` | Saldırı Taktikleri |
| Ralli Kapasitesi | `rally_capacity` | Saldırı Taktikleri |
| Sayaç Doktrini | `counter_doctrine` | Saldırı Taktikleri |
| Karşı Aydınlatma | `counter_illumination` | Saldırı Taktikleri |
| Kubbe Topçusu | `dome_battery` | Savunma Taktikleri |
| Kalkan Sertleştirme | `shield_hardening` | Savunma Taktikleri |
| Ambar Kilitleri | `vault_locks` | Savunma Taktikleri |
| Kubbe Savunma Doktrini | `dome_defense_doctrine` | Savunma Taktikleri |
| Hidrofon Dizisi | `hydrophone_array` | Savunma Taktikleri |
| Basınç Odası Rotasyonu | `chamber_rotation` | Savunma Taktikleri |
| Takviye Limanı | `reinforcement_dock` | Savunma Taktikleri |
| Yankısız Kaplama | `anechoic_tiles` | Savunma Taktikleri |
| Basınç Mayınları | `pressure_mines` | Savunma Taktikleri |
| Su Geçirmez Bölmeler | `watertight_bulkheads` | Savunma Taktikleri |
| Akustik Yem | `acoustic_decoys` | Savunma Taktikleri |
| 1. Hangar Ekibi | `hangar_1_crew` | Filo |
| Asker Bölmeleri | `troop_bays` | Filo |
| Kuru Havuz Robotları | `dry_dock_robots` | Filo |
| 2. Hangar Ekibi | `hangar_2_crew` | Filo |
| Hidrodinamik Gövde | `hydrodynamic_hull` | Filo |
| 3. Hangar Ekibi | `hangar_3_crew` | Filo |
| Kompozit Gövde Levhaları | `hull_plating` | Filo |
| Seri Gövde Üretimi | `serial_hull_production` | Filo |
| Basınca Dayanıklı Batarya | `pressure_tolerant_batteries` | Filo |
| Subay Eğitimi | `officer_training` | Filo |
| 4. Hangar Ekibi | `hangar_4_crew` | Filo |
| Filo Düzeni | `fleet_formation` | Filo |
| Konvoy Yük Kapasitesi | `convoy_capacity` | Konvoy |
| Derin Akıntı Rotaları | `deep_currents` | Konvoy |
| Takas Vergisi Muafiyeti | `trade_exemption` | Konvoy |
| Eskort Protokolü | `escort_protocol` | Konvoy |
| Ek Konvoy Hattı | `extra_convoy_line` | Konvoy |
| Kaçakçı Ağı | `smuggler_network` | Konvoy |
| Saçılma Katmanı Gizlemesi | `scattering_layer` | Konvoy |
| Tersane Rotasyonu | `shipyard_rotation` | Konvoy |
| Konvoy Sigortası | `convoy_insurance` | Konvoy |
| Lig: İnşa Günü | `league_build` | Derinlik Ligi |
| Lig: Araştırma Günü | `league_research` | Derinlik Ligi |
| Lig: Kahraman Günü | `league_hero` | Derinlik Ligi |
| Lig: Toplama Günü | `league_gather` | Derinlik Ligi |
| Lig: Eğitim Günü | `league_training` | Derinlik Ligi |
| Lig: Av Günü | `league_hunt` | Derinlik Ligi |
| Kademe Ödülleri | `league_tier_rewards` | Derinlik Ligi |
| Lig Koordinasyonu | `league_coordination` | Derinlik Ligi |
| Zıpkın Halatları | `harpoon_tethers` | Leviathan Avı |
| Leviathan İz Sürme | `hunt_tracking` | Leviathan Avı |
| Av Rallisi | `hunt_rally` | Leviathan Avı |
| Av Ganimeti | `hunt_loot` | Leviathan Avı |
| Biyolüminesans Fener | `bioluminescent_beacon` | Leviathan Avı |
| Demir Plaka Delici | `iron_plate_piercing` | Leviathan Avı |
| Av Sağlıkçıları | `hunt_medics` | Leviathan Avı |
| Akıntıyla Toplanma | `current_muster` | Leviathan Avı |
| Üçüncü Av Hattı | `third_hunt_line` | Leviathan Avı |
| Hadalit Rezonansı | `hadalite_resonance` | Kadim Teknoloji |
| Muhafız Şifreleri | `guardian_ciphers` | Kadim Teknoloji |
| Kadim Döküm | `ancient_casting` | Kadim Teknoloji |
| Kalıntı Haritacılığı | `relic_cartography` | Kadim Teknoloji |
| Kadim Kafes Zırh | `ancient_lattice` | Kadim Teknoloji |
| Rezonans Silahları | `resonance_weapons` | Kadim Teknoloji |
| Kadim Kalkan Matrisi | `ancient_shield_matrix` | Kadim Teknoloji |
| Kodeks Çözümleme | `codex_decryption` | Kadim Teknoloji |
| Kadim Çekirdek | `ancient_core` | Kadim Teknoloji |
| Titan Etüdü | `titan_studies` | Kadim Teknoloji |
<!-- /URET:sozluk-arastirma -->
