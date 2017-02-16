function initConfiguration() {

}

function toggleDemoMode(e) {
    if ($(e.currentTarget).attr("on") == "false") {
        $(e.currentTarget).attr("on", "true");
    }
    else {
        $(e.currentTarget).attr("on", "false");
    }
}
function toggleFileEncryption(e) {
    if ($(e.currentTarget).attr("on") == "false") {
        $(e.currentTarget).attr("on", "true");
    }
    else {
        $(e.currentTarget).attr("on", "false");
    }
}