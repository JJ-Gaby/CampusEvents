using CampusEvents.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace CampusEvents.Tests;

public class EventsControllerTests
{
    [Fact]
    public void Index_ReturnsViewWithExpectedTemporaryTitle()
    {
        var controller = new EventsController(NullLogger<EventsController>.Instance);

        var result = controller.Index();

        var view = Assert.IsType<ViewResult>(result);
        var title = Assert.IsType<string>(view.Model);
        Assert.Equal("Student Research Showcase", title);
    }
}
