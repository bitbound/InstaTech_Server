class Case {
}
var ConnectionType;
(function (ConnectionType) {
    ConnectionType[ConnectionType["Customer"] = 0] = "Customer";
    ConnectionType[ConnectionType["Technician"] = 1] = "Technician";
})(ConnectionType || (ConnectionType = {}));
class Main_Model {
    constructor() {
        this.Cases = [];
    }
}
const InstaTech = new Main_Model();
