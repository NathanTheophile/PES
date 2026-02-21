using System.Collections.Generic;
using NUnit.Framework;
using PES.Grid.Grid3D;
using PES.Grid.Pathfinding;

namespace PES.Tests.EditMode
{
    public sealed class PathfindingServiceTests
    {
        [Test]
        public void TryComputePath_WhenDirectLineBlocked_FindsDetour()
        {
            var service = new PathfindingService();
            var from = new GridCoord3(0, 0, 0);
            var to = new GridCoord3(2, 0, 0);
            var blocked = new HashSet<GridCoord3>
            {
                new(1, 0, 0),
            };

            var found = service.TryComputePath(from, to, blocked, out var path);

            Assert.That(found, Is.True);
            Assert.That(path[0], Is.EqualTo(from));
            Assert.That(path[path.Count - 1], Is.EqualTo(to));
            Assert.That(path, Has.No.Member(new GridCoord3(1, 0, 0)));
        }

        [Test]
        public void TryComputePath_WhenDestinationBlocked_ReturnsFalse()
        {
            var service = new PathfindingService();
            var from = new GridCoord3(0, 0, 0);
            var to = new GridCoord3(1, 0, 0);
            var blocked = new HashSet<GridCoord3>
            {
                to,
            };

            var found = service.TryComputePath(from, to, blocked, out var path);

            Assert.That(found, Is.False);
            Assert.That(path.Count, Is.EqualTo(1));
            Assert.That(path[0], Is.EqualTo(from));
        }
    }
}
