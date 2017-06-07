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
var Computer = (function () {
    function Computer() {
    }
    return Computer;
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
    Object.defineProperty(Main_Model.prototype, "HostAndPort", {
        get: function () {
            var hostAndPort = location.host;
            if (this.Secure_Socket_Port != "443") {
                if (location.protocol.search("https") > -1) {
                    hostAndPort = location.hostname + ":" + this.Secure_Socket_Port;
                }
            }
            if (this.Socket_Port != "80") {
                if (location.protocol.search("https") == -1) {
                    hostAndPort = location.hostname + ":" + this.Socket_Port;
                }
            }
            return hostAndPort;
        },
        enumerable: true,
        configurable: true
    });
    return Main_Model;
}());
var InstaTech = new Main_Model();
InstaTech.Socket_Port = "80";
InstaTech.Secure_Socket_Port = "443";
InstaTech.Temp = {};
//# sourceMappingURL=Main_Model.js.map