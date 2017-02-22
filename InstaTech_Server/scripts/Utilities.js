function showDialog(title, content) {
    var dialog = document.createElement("div");
    dialog.innerHTML = content;
    $(dialog).dialog({
        width: document.body.clientWidth * .5,
        title: title,
        classes: {
            "ui-dialog-title": "center-aligned",
            "ui-dialog": "ui-corner-all ui-widget ui-widget-content ui-front ui-dialog-buttons ui-draggable ui-resizable ui-widget-shadow",
        },
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
function showDialogEx(title, content, buttons) {
    var dialog = document.createElement("div");
    dialog.innerHTML = content;
    $(dialog).dialog({
        width: document.body.clientWidth * .5,
        title: title,
        classes: {
            "ui-dialog-title": "center-aligned",
            "ui-dialog": "ui-corner-all ui-widget ui-widget-content ui-front ui-dialog-buttons ui-draggable ui-resizable ui-widget-shadow",
        },
        buttons: buttons,
        close: function() {
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
function showTooltip(objPlacementTarget, strPlacementDirection, strColor, strMessage) {
    if (objPlacementTarget instanceof jQuery) {
        objPlacementTarget = objPlacementTarget[0];
    }
    var divTooltip = document.createElement("div");
    divTooltip.innerText = strMessage;
    divTooltip.classList.add("tooltip");
    divTooltip.style.zIndex = 3;
    divTooltip.id = "tooltip" + String(Math.random());
    $(divTooltip).css({
        "position": "absolute",
        "background-color": "whitesmoke",
        "color": strColor,
        "border-radius": "10px",
        "padding": "5px",
        "border": "1px solid dimgray",
        "font-size": ".8em",
        "box-shadow": "10px 5px 5px rgba(0,0,0,.2)"
    });
    var rectPlacement = objPlacementTarget.getBoundingClientRect();
    switch (strPlacementDirection) {
        case "top":
            {
                divTooltip.style.top = Number((rectPlacement.top + window.scrollY) - 5) + "px";
                divTooltip.style.transform = "translateY(-100%)";
                divTooltip.style.left = Number(rectPlacement.left + window.scrollX) + "px";
                break;
            }
        case "right":
            {
                divTooltip.style.top = Number(rectPlacement.top + window.scrollY) + "px";
                divTooltip.style.left = Number(rectPlacement.right + window.scrollX + 5) + "px";
                break;
            }
        case "bottom":
            {
                divTooltip.style.top = Number(rectPlacement.bottom + window.scrollY + 5) + "px";
                divTooltip.style.left = Number(rectPlacement.left + window.scrollX) + "px";
                break;
            }
        case "left":
            {
                divTooltip.style.top = Number(rectPlacement.top + window.scrollY) + "px";
                divTooltip.style.left = Number(rectPlacement.left + window.scrollX - 5) + "px";
                divTooltip.style.transform = "translateX(-100%)";
                break;
            }
        case "center":
            {
                divTooltip.style.top = Number(rectPlacement.bottom + window.scrollY - (rectPlacement.height / 2)) + "px";
                divTooltip.style.left = Number(rectPlacement.right + window.scrollX - (rectPlacement.width / 2)) + "px";
                divTooltip.style.transform = "translate(-50%, -50%)";
            }
        default:
            break;
    }
    $(document.body).append(divTooltip);
    window.setTimeout(function () {
        $(divTooltip).animate({ opacity: 0 }, 1000, function () {
            $(divTooltip).remove();
        });
    }, strMessage.length * 50);
}