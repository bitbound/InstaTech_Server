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
        ]
    });
}
