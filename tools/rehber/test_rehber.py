"""Kılavuz üreticisinin birim testleri: python -m unittest tools/rehber/test_rehber.py"""
import sys
import unittest
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

from rehber import bicim, egri, hesap, md, sim, v8, veri  # noqa: E402


class EgriTesti(unittest.TestCase):
    def test_capalarda_tam_deger(self):
        c = [[1, 100], [10, 1000]]
        self.assertAlmostEqual(egri.deger(c, 1), 100)
        self.assertAlmostEqual(egri.deger(c, 10), 1000)

    def test_log_dogrusal_orta(self):
        self.assertAlmostEqual(egri.deger([[0, 10], [2, 1000]], 1), 100)

    def test_uclarda_buyume_surer(self):
        self.assertAlmostEqual(egri.deger([[1, 10], [2, 20]], 3), 40)

    def test_sifirli_aralik_dogrusal(self):
        self.assertAlmostEqual(egri.deger([[0, 0], [10, 100]], 5), 50)

    def test_yuvarlama(self):
        self.assertEqual(egri.yuvarla_bedel(7.4), 7)
        self.assertEqual(egri.yuvarla_bedel(512), 510)
        self.assertEqual(egri.yuvarla_bedel(123456), 123000)
        self.assertEqual(egri.yuvarla_sure(95), 95)
        self.assertEqual(egri.yuvarla_sure(3700), 3720)
        self.assertEqual(egri.yuvarla_sure(90000), 90000)


class BicimTesti(unittest.TestCase):
    def test_sayi(self):
        self.assertEqual(bicim.sayi(1250000), "1.250.000")
        self.assertEqual(bicim.sayi(2.5), "2,5")
        self.assertEqual(bicim.sayi(-3), "−3")

    def test_yuzde(self):
        self.assertEqual(bicim.yuzde(4), "%4")
        self.assertEqual(bicim.yuzde(-3), "−%3")

    def test_sure(self):
        self.assertEqual(bicim.sure(45), "45 sn")
        self.assertEqual(bicim.sure(720), "12 dk")
        self.assertEqual(bicim.sure(7500), "2 sa 5 dk")
        self.assertEqual(bicim.sure(3 * 86400 + 4 * 3600), "3 g 4 sa")


class V8ParitesiTesti(unittest.TestCase):
    """docs/23_Base_Management.md ve C# formülleriyle aynı sonuçlar."""

    @classmethod
    def setUpClass(cls):
        cls.k = veri.yukle()["_katalog"]

    def test_reaktor_50(self):
        r = v8.bina(self.k, "core_reactor")
        self.assertEqual(v8.bina_bedel(self.k, r, 50), {"Titanium": 6822})
        self.assertEqual(v8.bina_sure(self.k, r, 50), 7741)

    def test_ittifak_merkezi_50(self):
        self.assertEqual(v8.bina_bedel(self.k, v8.bina(self.k, "alliance_hub"), 50)["Titanium"], 20465)

    def test_oksijen_uretimi(self):
        o = v8.bina(self.k, "oxygen_plant")
        self.assertEqual(v8.bina_uretim_saat(o, 1)["Oxygen"], 10800)
        self.assertEqual(v8.bina_uretim_saat(o, 50)["Oxygen"], 143100)

    def test_kahraman_etkisi_sifira_dogru_keser(self):
        mara = v8.kahraman(self.k, "mara")
        self.assertEqual(v8.kahraman_etki(mara, 2, 1), -3)
        self.assertEqual(v8.kahraman_etki(mara, 50, 5), -3 - 9 - 8)

    def test_arastirma(self):
        n = v8.arastirma_dugumu(self.k, "construction_mastery")
        s = v8.arastirma_seviye(self.k, n, 10)
        self.assertEqual(s["merkez"], 19)
        self.assertEqual(s["bedel"]["Titanium"], 1653)


class ModelTesti(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.m = hesap.Model(veri.yukle())

    def test_reaktor_on_kosul_donusumu(self):
        self.assertEqual(self.m.reaktor_on_kosul(7), [("shield_generator", 6), ("research_center", 6)])
        self.assertIn(("mega_dome", 2), self.m.reaktor_on_kosul(21))

    def test_kubbe_seviye_esleme(self):
        mega = self.m.binalar["mega_dome"]
        self.assertEqual(self.m.gerekli_reaktor(mega, 2), 20)
        self.assertEqual(self.m.sure(mega, 2), egri.yuvarla_sure(self.m.e("reaktor_sure", 20)))

    def test_f2p_bir_yil(self):
        s = sim.hepsi(self.m)
        self.assertTrue(340 <= s["f2p"]["gun_50"] <= 390, s["f2p"]["gun_50"])
        self.assertLess(s["balina"]["gun_50"], s["aylik_kart"]["gun_50"])
        self.assertLess(s["aylik_kart"]["gun_50"], s["f2p"]["gun_50"])


class IsaretTesti(unittest.TestCase):
    def test_blok_ve_deger_idempotent(self):
        kok = {"veri": {"x": {"a": [{"id": "p", "n": 1500}]}}}
        metin = "Önce\n<!-- URET:b -->eski<!-- /URET:b -->\nDeğer: <!--v:veri.x.a.p.n-->?<!--/v--> son"
        bir, h = md.isle(metin, lambda ad: "yeni", kok)
        iki, _ = md.isle(bir, lambda ad: "yeni", kok)
        self.assertEqual(h, [])
        self.assertEqual(bir, iki)
        self.assertIn("<!--v:veri.x.a.p.n-->1.500<!--/v-->", bir)
        self.assertTrue(bir.startswith("Önce\n"))


if __name__ == "__main__":
    unittest.main()
