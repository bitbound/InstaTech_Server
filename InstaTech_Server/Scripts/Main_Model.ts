class Case {
    CaseID: Number;
    DTCreated: Date;
    DTReceived: Date;
    DTClosed: Date;
    DTAbandoned: Date;
    CustomerFirstName: String;
    CustomerLastName: String;
    CustomerUserID: String;
    CustomerComputerName: String;
    CustomerPhone: String;
    CustomerEmail: String;
    SupportCategory: String;
    SupportType: String;
    SupportQueue: String;
    Details: String;
    Locked: Boolean;
    LockedBy: String;
}
enum ConnectionType {
    Customer,
    Technician
}
class Main_Model {
    constructor() {
        this.Cases = [];
    } 
    AuthenticationToken: string;
    Socket_Main: WebSocket;
    Cases: Case[];
    Context: ConnectionType;
    LastTypingStatus: Date;
    QueueWaitTimer: Number;
    UserID: string;
    PartnerFirstName: string;
    PartnerLastName: string;
}
const InstaTech = new Main_Model();