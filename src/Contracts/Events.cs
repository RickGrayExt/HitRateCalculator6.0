namespace Contracts;

public record StartRunCommand(Guid RunId, string DatasetPath, string Mode, RunParams Params);

public record SalesPatternsIdentified(Guid RunId, List<SkuDemand> Demand);
public record SkuGroupsCreated(Guid RunId, List<SkuGroup> Groups, List<SkuDemand> Demand);
public record ShelfLocationsAssigned(Guid RunId, List<ShelfLocation> Locations, List<SkuDemand> Demand);
public record RackLayoutCalculated(Guid RunId, List<Rack> Racks, List<ShelfLocation> Locations, List<SkuDemand> Demand);
public record BatchesCreated(Guid RunId, List<Batch> Batches, string Mode);
public record StationsAllocated(Guid RunId, List<StationAssignment> Assignments);
public record HitRateCalculated(Guid RunId, HitRateResult Result);
