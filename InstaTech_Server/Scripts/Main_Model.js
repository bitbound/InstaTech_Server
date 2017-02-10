var Case = (function () {
    function Case() {
    }
    return Case;
}());
var Tech_Account = (function () {
    function Tech_Account() {
    }
    return Tech_Account;
}());
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
var Main_Model = (function () {
    function Main_Model() {
        this.Cases = [];
    }
    return Main_Model;
}());
var InstaTech = new Main_Model();
//# sourceMappingURL=Main_Model.js.map