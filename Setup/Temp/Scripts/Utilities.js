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

function showLoading() {
    if (document.getElementById("divLoadingFrame") != null) {
        return;
    }
    var style = document.createElement("style");
    style.id = "styleLoadingWindow";
    style.innerHTML = `
        .loading-frame {
            position: fixed;
            background-color: rgba(0, 0, 0, 0.8);
            left: 0;
            top: 0;
            right: 0;
            bottom: 0;
            z-index: 4;
        }
        .loading-track {
            height: 50px;
            display: inline-block;
            position: absolute;
            top: calc(50% - 50px);
            left: 50%;
        }
        .loading-dot {
            height: 5px;
            width: 5px;
            background-color: white;
            border-radius: 100%;
            opacity: 0;
        }
        .loading-dot-animated {
            animation-name: loading-dot-animated;
            animation-direction: alternate;
            animation-duration: .75s;
            animation-iteration-count: infinite;
            animation-timing-function: ease-in-out;
        }
        @keyframes loading-dot-animated {
            from {
                opacity: 0;
            }
            to {
                opacity: 1;
            }
        }
    `
    document.body.appendChild(style);
    var frame = document.createElement("div");
    frame.id = "divLoadingFrame";
    frame.classList.add("loading-frame");
    for (var i = 0; i < 10; i++) {
        var track = document.createElement("div");
        track.classList.add("loading-track");
        var dot = document.createElement("div");
        dot.classList.add("loading-dot");
        track.style.transform = "rotate(" + String(i * 36) + "deg)";
        track.appendChild(dot);
        frame.appendChild(track);
    }
    document.body.appendChild(frame);
    var wait = 0;
    var dots = document.getElementsByClassName("loading-dot");
    for (var i = 0; i < dots.length; i++) {
        window.setTimeout(function (dot) {
            dot.classList.add("loading-dot-animated");
        }, wait, dots[i]);
        wait += 150;
    }
};
function removeLoading() {
    document.body.removeChild(document.getElementById("divLoadingFrame"));
    document.body.removeChild(document.getElementById("styleLoadingWindow"));
};