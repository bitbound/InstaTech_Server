function initCustomerRemote() {
    $("#divCustomerRemote").position({
        my: "center top",
        at: "center top",
        of: '#divCustomerContent'
    });
    $("#divCustomerRemote").draggable();
}

function downloadCustomerClient(e, arrPaths) {
    var path = arrPaths[$("#selectCustomerClientVersion").val()];
    window.open(path);
}