using System;
using System.Linq;
using HADAL.Shared.Commands;
using HADAL.Shared.Enums;
using Hadal.Core.Network;
using NUnit.Framework;

namespace Hadal.Tests.EditMode
{
    /// <summary>
    /// A command type the dispatcher does not know goes out as Unknown and the server ignores it; 1.7.0–1.7.2
    /// shipped gathering, dives and alliance management that way. Every shared command must be mapped.
    /// </summary>
    public sealed class CommandMappingTests
    {
        [Test]
        public void Every_command_type_is_mapped()
        {
            var commands = typeof(ICommand).Assembly.GetTypes()
                .Where(t => typeof(ICommand).IsAssignableFrom(t) && t.IsClass && !t.IsAbstract)
                .ToList();
            Assert.That(commands.Count, Is.GreaterThan(30));

            var unmapped = commands
                .Where(t => CommandDispatcher.ResolveCommandType((ICommand)Activator.CreateInstance(t)) == CommandType.Unknown)
                .Select(t => t.Name)
                .ToList();
            Assert.That(unmapped, Is.Empty, "Add to CommandDispatcher.ResolveCommandType: " + string.Join(", ", unmapped));
        }
    }
}
