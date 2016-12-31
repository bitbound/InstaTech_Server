$(document).ready(function () {
    if (!init()) {
        $(".portal-button-frame").remove();
        return;
    }
    setPortalButtonHandlers(); 
});

function init() {
    if (typeof WebSocket == "undefined")
    {
        $("#divWebSocketUnavailable").show();
        return false;
    }
    try
    {
        var protocol = location.protocol.replace("http:", "ws:").replace("https:", "wss:") + "//";
        InstaTech.ChatSocket = new WebSocket(protocol + location.host + "/Services/Chat_Socket.cshtml");
        setChatSocketHandlers();
        return true;
    }
    catch (ex)
    {
        $("#divWebSocketError").show();
        return false;
    }
}

function switchToTechPortal() {
    $("#divCustomerPortal").slideUp(function () {
        $("#divTechPortal").slideDown();
    });
}

function switchToCustomerPortal() {
    $("#divTechPortal").slideUp(function () {
        $("#divCustomerPortal").slideDown();
    });
}

function setPortalButtonHandlers() {
    $(".portal-option-button").click(function () {
        $(this).addClass("remove-css");
        $(this).css({
            "transform": "scale(.75, .75)",
            "transition-duration": ".4s",
            "transition-timing-function": "ease",
            "z-index": 2,
        });
        window.setTimeout(function () {
            $(".remove-css").attr("style", "");
            $(".remove-css").removeClass("remove-css");
        }, 500);
        var strOpens = $(this).attr("opens");
        if ($(strOpens).length == 0) {
            var strFile = $(this).attr("opens-file");
            $.get("/Controls/" + strFile, function (data) {
                $("#divCustomerContent").append(data);
                slideToggleContent(strOpens);
            });
        }
        else
        {
            slideToggleContent(strOpens);
        }
    })
}
function slideToggleContent(strElementName) {
    var strOpens = strElementName;
    if (strOpens.search("#") != 0)
    {
        strOpens = "#" + strOpens;
    }
    var opens = $(strOpens);
    if (opens.is(":visible")) {
        opens.slideUp();
    }
    else {
        opens.slideDown(function () {
            window.scroll(0, document.body.scrollHeight);
            opens.find("input").first().select();
        });
    }
}

function setChatSocketHandlers() {
    InstaTech.ChatSocket.onopen = function () {

    }
    InstaTech.ChatSocket.onmessage = function () {

    }
    InstaTech.ChatSocket.onclose = function () {

    }
    InstaTech.ChatSocket.onerror = function () {
        
    }
}