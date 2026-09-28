using Application.DTOs;
using Application.Projects;
using Domain.Posts;

namespace Tests;

public class ProjectStatusTests
{
    private static DateTimeOffset DaysAgo(int days) => DateTimeOffset.UtcNow.AddDays(-days);

    [Fact]
    public void WithinTheWindow_IsActive()
    {
        Assert.Equal(ProjectStatus.Active, ProjectStatusRules.Derive(DaysAgo(1)));
        Assert.Equal(ProjectStatus.Active, ProjectStatusRules.Derive(DaysAgo(364)));
    }

    [Fact]
    public void OutsideTheWindow_IsArchived()
    {
        Assert.Equal(ProjectStatus.Archived, ProjectStatusRules.Derive(DaysAgo(366)));
    }

    [Fact]
    public void Null_IsArchived()
    {
        // Fails closed: an unknown last push must never claim to be active.
        Assert.Equal(ProjectStatus.Archived, ProjectStatusRules.Derive(null));
    }

    [Fact]
    public void SummaryResponse_CarriesNoContent()
    {
        var exposed = typeof(ProjectSummaryResponse).GetProperties().Select(p => p.Name).ToHashSet();

        Assert.DoesNotContain("Content", exposed);
    }

    [Fact]
    public void Responses_ExposeNoInternalFields()
    {
        foreach (var type in new[] { typeof(ProjectSummaryResponse), typeof(ProjectResponse) })
        {
            var exposed = type.GetProperties().Select(p => p.Name).ToHashSet();
            Assert.DoesNotContain("IsPublished", exposed);
            Assert.DoesNotContain("CreatedOn", exposed);
            Assert.DoesNotContain("UpdatedOn", exposed);
        }
    }

    [Fact]
    public void FromDomain_ComputesStatus()
    {
        var project = Project.Create(
            Guid.NewGuid(), "T", "t", "body", "brief", ProjectCategory.Graphics, 2025);
        project.LastPushedAt = DaysAgo(2);

        Assert.Equal(ProjectStatus.Active, ProjectResponse.FromDomain(project).Status);
        Assert.Equal(ProjectStatus.Active, ProjectSummaryResponse.FromDomain(project).Status);
    }

    [Fact]
    public void FromDomain_CarriesTheTypedFields()
    {
        var project = Project.Create(
            Guid.NewGuid(), "T", "t", "body", "brief", ProjectCategory.BusinessCase, 2024);
        project.Stack = ["C#", "Vue"];
        project.CanonicalUrl = "https://example.com";
        project.Links = [new ProjectLink { Label = "Source", Url = "https://github.com/x/y" }];

        var response = ProjectResponse.FromDomain(project);

        Assert.Equal(ProjectCategory.BusinessCase, response.Category);
        Assert.Equal(2024, response.Year);
        Assert.Equal(["C#", "Vue"], response.Stack);
        Assert.Equal("https://example.com", response.CanonicalUrl);
        Assert.Single(response.Links);
        Assert.Equal("Source", response.Links[0].Label);
    }
}
