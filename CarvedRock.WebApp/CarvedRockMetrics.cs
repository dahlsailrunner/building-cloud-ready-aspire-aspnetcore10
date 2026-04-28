using System.Diagnostics.Metrics;

namespace CarvedRock.WebApp;

// https://opentelemetry.io/docs/zero-code/dotnet/custom/#metrics
// https://learn.microsoft.com/en-us/dotnet/core/diagnostics/metrics-instrumentation
public class CarvedRockMetrics
{
    private readonly Counter<int> _listingViews;
    public CarvedRockMetrics(IMeterFactory meterFactory)
    {        
        var meter = meterFactory.Create("CarvedRock.WebApp");
        _listingViews = meter.CreateCounter<int>("carvedrock.listingpage.views",
            unit: "Page Views",
            description: "Counts the number of times the listing page has " + 
            "been viewed since the metric has been started.");
    }

    public void ListingPageWasViewed()
    {
        _listingViews.Add(1);
    }
}