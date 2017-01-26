function showDialog(title, content) {
    var dialog = document.createElement("div");
    dialog.innerHTML = content;
    $(dialog).dialog({
        width: document.body.clientWidth * .5,
        title: title,
        classes: { "ui-dialog-title": "center-aligned" },
        buttons: [
            {
                text: "OK",
                click: function () {
                    $(this).dialog("close");
                }
            }
        ],
        close: function () {
            $(this).dialog('destroy').remove();
        }
    });
}
function parseNETDate(strDate) {
    return new Date(parseInt(strDate.replace("/Date(", "").replace(")/", "")));
}
function sendClientError(strError) {
    // TODO: Client-side error sending?
    console.log(strError);
}
function getTimeSince(dateNETDateSince) {
    var totalSec = Math.round((new Date() - parseNETDate(dateNETDateSince)) / 1000);
    var secs = totalSec % 60;
    var mins = Math.floor(totalSec / 60);
    return mins + "m, " + secs + "s";
}