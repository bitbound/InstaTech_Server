class Case {
}
class Tech_Account {
}
class Computer {
}
var AccessLevel;
(function (AccessLevel) {
    AccessLevel[AccessLevel["Standard"] = 0] = "Standard";
    AccessLevel[AccessLevel["Admin"] = 1] = "Admin";
})(AccessLevel || (AccessLevel = {}));
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
InstaTech.Temp = {};
//# sourceMappingURL=Main_Model.js.map