class Case {
    CaseID: Number;
    DTCreated: Date;
    DTReceived: Date;
    DTClosed: Date;
    DTAbandoned: Date;
    CustomerFirstName: String;
    CustomerLastName: String;
    CustomerUserID: String;
    CustomerComputerID: String;
    CustomerPhone: String;
    CustomerEmail: String;
    SupportCategory: String;
    SupportType: String;
    SupportQueue: String;
    Details: String;
}
class Main_Model {
    constructor() {
        this.Cases = [];
    } 
    Socket_Chat: WebSocket;
    Cases: Case[];

}
const InstaTech = new Main_Model();