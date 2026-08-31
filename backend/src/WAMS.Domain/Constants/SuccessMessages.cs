namespace WAMS.Domain.Constants;

public static class SuccessMessages
{
    public static class General
    {
        public const string DataRetrieved = "Data retrieved";
        public const string OperationSuccessful = "Operation successful";
        public const string HistoryRetrieved = "History retrieved";
        public const string AvailableItemsRetrieved = "Available items retrieved";
        public const string ApprovedBudgetPlansRetrieved = "Approved budget plans retrieved";
    }

    public static class Auth
    {
        public const string LoginSuccessful = "Login successful";
        public const string TokenRefreshed = "Token refreshed";
        public const string LoggedOut = "Logged out successfully";
    }

    public static class Dashboard
    {
        public const string SummaryRetrieved = "Dashboard summary retrieved";
        public const string ActivitiesRetrieved = "Today's activities retrieved";
        public const string HistoryRetrieved = "Dashboard history retrieved";
    }

    public static class AccountPayable
    {
        public const string ApprovedRecapsRetrieved = "Approved recaps retrieved";
        public const string ListRetrieved = "Account payables retrieved";
        public const string Retrieved = "Account payable retrieved";
        public const string Created = "Account payable created";
        public const string CreatedAndGenerated = "Account payable created and generated";
        public const string Updated = "Account payable updated";
        public const string Generated = "Account payable generated";
    }

    public static class ActivityType
    {
        public const string ListRetrieved = "Activity types retrieved";
        public const string Retrieved = "Activity type retrieved";
        public const string Created = "Activity type created";
        public const string Updated = "Activity type updated";
    }

    public static class AuditLog
    {
        public const string ListRetrieved = "Audit logs retrieved";
        public const string Retrieved = "Audit log retrieved";
        public const string RecordHistory = "Record history retrieved";
    }

    public static class BudgetPlan
    {
        public const string ListRetrieved = "Budget plans retrieved";
        public const string Retrieved = "Budget plan retrieved";
        public const string Created = "Budget plan created";
        public const string CreatedAndSubmitted = "Budget plan created and submitted";
        public const string Updated = "Budget plan updated";
        public const string SpkItemAdded = "SPK item added";
    }

    public static class BudgetTemplate
    {
        public const string ListRetrieved = "Budget templates retrieved";
        public const string Retrieved = "Budget template retrieved";
        public const string Created = "Budget template created";
        public const string CreatedAndSubmitted = "Budget template created and submitted";
        public const string Updated = "Budget template updated";
    }

    public static class Company
    {
        public const string ListRetrieved = "Companies retrieved";
        public const string Retrieved = "Company retrieved";
        public const string Created = "Company created";
        public const string Updated = "Company updated";
        public const string Deactivated = "Company deactivated";
        public const string UserAssigned = "User assigned to company";
        public const string LogoUploaded = "Logo uploaded";
    }

    public static class File
    {
        public const string Uploaded = "Files uploaded";
        public const string Retrieved = "Files retrieved";
    }

    public static class FinanceReport
    {
        public const string ListRetrieved = "Finance reports retrieved";
        public const string Retrieved = "Finance report retrieved";
    }

    public static class Item
    {
        public const string ListRetrieved = "Items retrieved";
        public const string Retrieved = "Item retrieved";
    }

    public static class Notification
    {
        public const string ListRetrieved = "Notifications retrieved";
        public const string TestDispatched = "Test notification dispatched";
    }

    public static class Permission
    {
        public const string ListRetrieved = "Permissions retrieved";
    }

    public static class PurchaseOrder
    {
        public const string ListRetrieved = "Purchase orders retrieved";
        public const string Retrieved = "Purchase order retrieved";
        public const string Created = "Purchase order created";
        public const string CreatedAndGenerated = "Purchase order created and generated";
        public const string Updated = "Purchase order updated";
        public const string Generated = "Purchase order generated";
        public const string ApdpGenerated = "Purchase order APDP generated";
        public const string RecapListRetrieved = "Purchase order recap list retrieved";
        public const string RecapRetrieved = "Purchase order recap retrieved";
    }

    public static class RateCard
    {
        public const string VendorRatesRetrieved = "Vendor rates retrieved";
        public const string ListRetrieved = "Rate cards retrieved";
        public const string Retrieved = "Rate card retrieved";
        public const string Created = "Rate card created";
        public const string CreatedAndSubmitted = "Rate card created and submitted";
        public const string Updated = "Rate card updated";
        public const string Submitted = "Rate card submitted";
    }

    public static class RecapWorkOrder
    {
        public const string ListRetrieved = "Recap work orders retrieved";
        public const string Retrieved = "Recap work order retrieved";
        public const string Approved = "Recap work order approved";
        public const string Rejected = "Recap work order rejected";
    }

    public static class Role
    {
        public const string ListRetrieved = "Roles retrieved";
        public const string Retrieved = "Role retrieved";
        public const string Created = "Role created";
        public const string Updated = "Role updated";
        public const string Deleted = "Role deleted";
        public const string PermissionsUpdated = "Role permissions updated";
        public const string PermissionAssigned = "Permission assigned";
        public const string PermissionRemoved = "Permission removed";
    }

    public static class Spk
    {
        public const string ListRetrieved = "SPK list retrieved";
        public const string Retrieved = "SPK retrieved";
    }

    public static class Sync
    {
        public const string Completed = "Sync completed";
        public const string LatestPerService = "Latest sync per service";
        public static string ServiceCompleted(string serviceName) => $"{serviceName} sync completed";
    }

    public static class TransportOrder
    {
        public const string ListRetrieved = "Transport orders retrieved";
        public const string Retrieved = "Transport order retrieved";
    }

    public static class Uom
    {
        public const string ListRetrieved = "UoMs retrieved";
        public const string Retrieved = "UoM retrieved";
        public const string Created = "UoM created";
        public const string Updated = "UoM updated";
        public const string Deleted = "UoM deleted";
    }

    public static class TaxType
    {
        public const string ListRetrieved = "Tax types retrieved";
        public const string Retrieved = "Tax type retrieved";
        public const string Created = "Tax type created";
        public const string Updated = "Tax type updated";
        public const string Deleted = "Tax type deactivated";
    }

    public static class User
    {
        public const string ListRetrieved = "Users retrieved";
        public const string Retrieved = "User retrieved";
        public const string Created = "User created";
        public const string Updated = "User updated";
        public const string Deleted = "User deleted";
        public const string PasswordChanged = "Password changed";
        public const string RoleAssigned = "Role assigned";
        public const string RoleRemoved = "Role removed";
        public const string WarehouseAssigned = "Warehouse assigned";
        public const string WarehouseRemoved = "Warehouse removed";
        public const string PermissionOverridesRetrieved = "Permission overrides retrieved";
        public const string PermissionGranted = "Permission granted";
        public const string PermissionDenied = "Permission denied";
        public const string PermissionOverrideRemoved = "Permission override removed";
        public const string EffectivePermissionsRetrieved = "Effective permissions retrieved";
    }

    public static class Vendor
    {
        public const string ListRetrieved = "Vendors retrieved";
        public const string Retrieved = "Vendor retrieved";
    }

    public static class Warehouse
    {
        public const string ListRetrieved = "Warehouses retrieved";
        public const string Retrieved = "Warehouse retrieved";
        public const string LocationsRetrieved = "Locations retrieved";
        public const string UnmappedRetrieved = "Unmapped warehouses retrieved";
    }

    public static class WorkflowTemplate
    {
        public const string DocumentTypesRetrieved = "Document types retrieved";
        public const string ListRetrieved = "Workflow templates retrieved";
        public const string Retrieved = "Workflow template retrieved";
        public const string Created = "Workflow template created";
        public const string Updated = "Workflow template updated";
    }

    public static class WorkOrder
    {
        public const string ListRetrieved = "Work orders retrieved";
        public const string Retrieved = "Work order retrieved";
        public const string Created = "Work order created";
        public const string Updated = "Work order updated";
        public const string Submitted = "Work order submitted";
        public const string PicCandidatesRetrieved = "Work order PIC candidates retrieved";
    }
}
