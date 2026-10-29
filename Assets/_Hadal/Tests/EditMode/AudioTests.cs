using System;
using HADAL.Shared.Enums;
using Hadal.Core.Services;
using Hadal.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Hadal.Tests.EditMode
{
    /// <summary>Every track and effect the game asks for has a clip, imported the way mobile wants it.</summary>
    public sealed class AudioTests
    {
        [Test]
        public void Every_music_track_and_sting_has_a_streaming_clip()
        {
            foreach (MusicTrack track in Enum.GetValues(typeof(MusicTrack)))
            {
                if (track == MusicTrack.None)
                    continue;
                AssertClip(GameAudio.MusicFolder + GameAudio.MusicClip(track), AudioClipLoadType.Streaming, 30f);
            }

            AssertClip(GameAudio.MusicFolder + "victory", AudioClipLoadType.Streaming, 2f);
            AssertClip(GameAudio.MusicFolder + "defeat", AudioClipLoadType.Streaming, 2f);
        }

        [Test]
        public void Every_effect_has_a_short_clip_decoded_on_load()
        {
            foreach (Sfx sfx in Enum.GetValues(typeof(Sfx)))
            {
                var clip = AssertClip(GameAudio.SfxFolder + GameAudio.SfxClip(sfx), AudioClipLoadType.DecompressOnLoad, 0.05f);
                Assert.That(clip.length, Is.LessThan(3f), sfx.ToString());
            }
        }

        [Test]
        public void Effect_names_are_snake_case()
        {
            Assert.That(GameAudio.SfxClip(Sfx.UiTap), Is.EqualTo("ui_tap"));
            Assert.That(GameAudio.SfxClip(Sfx.ResearchComplete), Is.EqualTo("research_complete"));
        }

        [Test]
        public void Confirmed_commands_map_to_their_effects()
        {
            Assert.That(AudioDirector.Effect(CommandType.FinishConstruction), Is.EqualTo(Sfx.BuildComplete));
            Assert.That(AudioDirector.Effect(CommandType.MarchAttack), Is.EqualTo(Sfx.MarchLaunch));
            Assert.That(AudioDirector.Effect(CommandType.Teleport), Is.EqualTo(Sfx.Teleport));
            Assert.That(AudioDirector.Effect(CommandType.ClaimVipChest), Is.EqualTo(Sfx.VipClaim));
            Assert.That(AudioDirector.Effect(CommandType.OpenItem), Is.EqualTo(Sfx.ChestOpen));
            Assert.That(AudioDirector.Effect(CommandType.SendChat), Is.Null);
        }

        private static AudioClip AssertClip(string resource, AudioClipLoadType loadType, float minSeconds)
        {
            var clip = Resources.Load<AudioClip>(resource);
            Assert.That(clip, Is.Not.Null, resource);
            Assert.That(clip.length, Is.GreaterThanOrEqualTo(minSeconds), resource);
            var importer = (AudioImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(clip));
            Assert.That(importer.defaultSampleSettings.loadType, Is.EqualTo(loadType), resource);
            return clip;
        }
    }
}
