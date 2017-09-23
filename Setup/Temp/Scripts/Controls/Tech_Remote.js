function initTechRemote() {
    $("#divTechRemote").position({
        my: "center top",
        at: "center top",
        of: '#divTechContent'
    });
    $("#divTechRemote").draggable();
}

function downloadTechClient(e, arrPaths) {
    var path = arrPaths[$("#selectTechClientVersion").val()];
    window.open(path);
}