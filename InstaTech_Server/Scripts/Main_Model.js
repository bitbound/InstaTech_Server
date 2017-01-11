var Case = (function () {
    function Case() {
    }
    return Case;
}());
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
