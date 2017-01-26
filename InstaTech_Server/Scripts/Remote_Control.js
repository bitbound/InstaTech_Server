var context = {};
var socket = {};
var rtcConnection;
var img;
var byteArray;
var imageData;
var imgWidth;
var imgHeight;
var imgX;
var imgY;
var url;
var fr = new FileReader();
var currentTouches = 0;
var cancelNextTouch;
var lastMultiTouch;
var lastTouch;
var multiTouched;
var doubleTapped;
var touchDragging;
var lastTouchPointX;
var lastTouchPointY;
function submitTechMainLogin(e) {
    if (e) {
        e.preventDefault();
    }
    var formData = {};
    $("#formTechMainLogin").find("input").each(function (index, item) {
        if (item.type == "checkbox") {
            formData[item.name] = item.checked;
        }
        else {
            formData[item.name] = item.value;
        }
    });
    if (InstaTech.AuthenticationToken) {
        formData["AuthenticationToken"] = InstaTech.AuthenticationToken;
    }
    socket.send(JSON.stringify(formData));
}
function logOutTech(e) {
    clearCachedCreds();
    socket.close();
    $("#spanMainLoginStatus").html("<small>Not logged in.</small>");
    $("#aMainTechLogOut").hide();
    $("#aMainTechLogIn").show();
}
function clearCachedCreds() {
    InstaTech.UserID = null;
    InstaTech.AuthenticationToken = null;
    localStorage.removeItem("RememberMe");
    localStorage.removeItem("UserID");
    localStorage.removeItem("AuthenticationToken");
}
function setMainLoginFrame() {
    $("#spanMainLoginStatus").html("<small>Logged in as: " + InstaTech.UserID + "</small>");
    $("#aMainTechLogIn").hide();
    $("#aMainTechLogOut").show();
}
function forgotPasswordMain(e) {
    if ($("#inputTechMainUserID").val().length == 0) {
        showDialog("User ID Required", "You must first enter a user ID into the form before you can reset the password.");
        return;
    }
    var dialog = document.createElement("div");
    dialog.innerHTML = "This will reset your password and send a temporary password to your email.<br/><br/>Proceed?";
    $(dialog).dialog({
        width: document.body.clientWidth * .5,
        title: "Confirm Password Reset",
        classes: { "ui-dialog-title": "center-aligned" },
        buttons: [
            {
                text: "Yes",
                click: function () {
                    var request = {
                        "Type": "ForgotPassword",
                        "UserID": $("#inputTechMainUserID").val()
                    };
                    socket.send(JSON.stringify(request));
                    $(this).dialog("close");
                }
            },
            {
                text: "No",
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
function switchToCustomerPortal() {
    $("#divTechPortal").fadeOut(function () {
        $("#divCustomerPortal").fadeIn();
    });
}
function connectToClient() {
    if (socket.readyState == WebSocket.CLOSED || socket.readyState == WebSocket.CLOSING)
    {
        $("#divStatus").text("Reconnecting...");
        initWebSocket();
        window.setTimeout(connectToClient, 1000);
        return;
    }
    context.canvas.width = 1;
    context.canvas.height = 1;
    connected = true;
    $("#divStatus").text("Connecting...");

    var request = {
        "Type": "ConnectionType",
        "ConnectionType": "ViewerApp"
    };
    socket.send(JSON.stringify(request));
    if ($(".toggle-box.selected").attr("id") == "divInteractive") {
        var sessionID = $("#inputSessionID").val();
        request = {
            "Type": "Connect",
            "SessionID": sessionID
        };
        socket.send(JSON.stringify(request));
    }
    else {
        var computerName = $("#inputSessionID").val();
        request = {
            "Type": "ConnectUnattended",
            "ComputerName": computerName,
            "AuthenticationToken": InstaTech.AuthenticationToken
        };
        socket.send(JSON.stringify(request));
    }
}
function searchComputers(e) {
    if ($("#divSearchComputers > div").is(":visible"))
    {
        $("#divSearchComputers > div").slideUp();
    }
    else
    {
        var request = {
            "Type": "SearchComputers",
            "AuthenticationToken": InstaTech.AuthenticationToken
        };
        socket.send(JSON.stringify(request));
    }
}
function filterComputers() {
    var searchStr = $("#inputFilterComputers").val();
    $("#selectComputers").children().each(function (index, elem) {
        if (elem.innerHTML.search(searchStr) == -1)
        {
            elem.setAttribute("hidden", true);
        }
        else
        {
            elem.removeAttribute("hidden");
        }
    })
}
function selectComputer() {
    $("#inputSessionID").val($("#selectComputers").val());
    $("#divSearchComputers > div").slideUp();
}

function initWebSocket() {
    if (window.location.href.search("localhost") > -1) {
        socket = new WebSocket("ws://" + location.host + location.pathname.toLowerCase().split("/remote_control")[0] + "/Services/Remote_Control_Socket.cshtml");
    }
    else {
        socket = new WebSocket("wss://" + location.host + location.pathname.toLowerCase().split("/remote_control")[0] + "/Services/Remote_Control_Socket.cshtml");
    }
    socket.binaryType = "arraybuffer";
    socket.onopen = function (e) {
        if (localStorage["RememberMe"]) {
            InstaTech.UserID = localStorage["UserID"];
            InstaTech.AuthenticationToken = localStorage["AuthenticationToken"];
            $("#inputTechMainUserID").val(InstaTech.UserID);
            $("#inputTechMainPassword").val("**********");
            document.getElementById("inputTechMainRememberMe").checked = true;
            submitTechMainLogin();
        }
    };
    socket.onclose = function (e) {
        $("#divMain").hide();
        $("#canvasRemoteControl").hide();
        $("#videoRemoteControl").hide();
        $("#divConnect").show();
        $("#divStatus").text("Session closed.");
        initWebSocket();
    };
    socket.onerror = function (e) {
        $("#divMain").hide();
        $("#canvasRemoteControl").hide();
        $("#videoRemoteControl").hide();
        $("#divConnect").show();
        $("#divStatus").text("Session closed due to an error.");
        initWebSocket();
    };
    socket.onmessage = function (e) {
        if (e.data instanceof ArrayBuffer) {
            if ($("#canvasRemoteControl").is(":hidden")) {
                $("#canvasRemoteControl").show();
            }
            byteArray = new Uint8Array(e.data);
            var length = byteArray.length;
            imgX = Number(byteArray[length - 4] * 100 + byteArray[length - 3]);
            imgY = Number(byteArray[length - 2] * 100 + byteArray[length - 1]);
            url = window.URL.createObjectURL(new Blob([byteArray.subarray(0, length - 4)]));
            img = document.createElement("img");
            img.onload = function () {
                context.drawImage(img, imgX, imgY, img.width, img.height);
                window.URL.revokeObjectURL(url);
            };
            img.src = url;
        }
        else {
            var jsonMessage = JSON.parse(e.data);
            switch (jsonMessage.Type) {
                case "TechMainLogin":
                    if (jsonMessage.Status == "new required") {
                        $("#inputTechMainConfirmNewPassword, #inputTechMainNewPassword").attr("required", true);
                        $("#inputTechMainConfirmNewPassword, #inputTechMainNewPassword").parent("td").parent("tr").show();
                        return;
                    }
                    else if (jsonMessage.Status == "ok") {
                        clearCachedCreds();
                        InstaTech.Context = "Technician";
                        InstaTech.UserID = $("#inputTechMainUserID").val();
                        InstaTech.AuthenticationToken = jsonMessage.AuthenticationToken;
                        if (document.getElementById("inputTechMainRememberMe").checked) {
                            localStorage["RememberMe"] = true;
                            localStorage["UserID"] = InstaTech.UserID;
                            localStorage["AuthenticationToken"] = InstaTech.AuthenticationToken;
                        }
                        $("#divMainTechLoginForm").slideUp();
                        setMainLoginFrame();
                    }
                    else if (jsonMessage.Status == "invalid") {
                        clearCachedCreds();
                        showDialog("Incorrect Credentials", "The user ID or password is incorrect.  Please try again.");
                        return;
                    }
                    else if (jsonMessage.Status == "locked") {
                        showDialog("Account Locked", "Your account as been locked due to failed login attempts.  It will unlock automatically after 10 minutes.  Please try again later.");
                        return;
                    }
                    else if (jsonMessage.Status == "temp ban") {
                        showDialog("Temporary Ban", "Due to failed login attempts, you must refresh your browser to try again.");
                        return;
                    }
                    else if (jsonMessage.Status == "password mismatch") {
                        showDialog("Password Mismatch", "The passwords you entered don't match.  Please retype them.");
                        return;
                    }
                    else if (jsonMessage.Status == "password length") {
                        showDialog("Password Length", "Your new password must be between 8 and 20 characters long.");
                        return;
                    }
                    break;
                case "Unauthorized":
                    $("#divStatus").text("");
                    showDialog("Access Denied", jsonMessage.Reason);
                    break;
                case "Connect":
                    $("#divStatus").text("");
                    if (jsonMessage.Status == "ok") {
                        // Initialize RTC and attempt to connect.
                        initRTC();
                        $("#divConnect").hide();
                        $("#divMain").show();
                    }
                    else if (jsonMessage.Status == "InvalidID") {
                        $("#divMain").hide();
                        $("#divConnect").show();
                        showTooltip($("#inputSessionID"), "bottom", "red", "Session ID not found.");

                    }
                    else if (jsonMessage.Status == "AlreadyHasPartner") {
                        $("#divMain").hide();
                        $("#divConnect").show();
                        showTooltip($("#inputSessionID"), "bottom", "red", "That client already has a partner connected.");
                    }
                    break;
                case "ConnectUnattended":
                    $("#divStatus").text("");
                    if (jsonMessage.Status == "ok") {
                        // Initialize RTC and attempt to connect.
                        initRTC();
                        $("#divConnect").hide();
                        $("#divMain").show();
                    }
                    if (jsonMessage.Status == "UnknownComputer") {
                        $("#divMain").hide();
                        $("#divConnect").show();
                        showTooltip($("#inputSessionID"), "bottom", "red", "Computer name not found.");
                    }
                    if (jsonMessage.Status == "AlreadyHasPartner") {
                        $("#divMain").hide();
                        $("#divConnect").show();
                        showTooltip($("#inputSessionID"), "bottom", "red", "That client already has a partner connected.");
                    }
                    break;
                case "ProcessStartResult":
                    if (jsonMessage.Status == "ok")
                    {
                        var computerName = $("#inputSessionID").val();
                        request = {
                            "Type": "ConnectUnattended",
                            "ComputerName": computerName,
                            "AuthenticationToken": InstaTech.AuthenticationToken
                        };
                        socket.send(JSON.stringify(request));
                    }
                    else if (jsonMessage.Status == "failed")
                    {
                        showDialog("Connection Failed", "Failed to connect to the remote computer.");
                    }
                    break;
                case "SearchComputers":
                    var selectEle = document.getElementById("selectComputers");
                    selectEle.innerHTML = "";
                    jsonMessage.Computers.forEach(function (value, index) {
                        var option = document.createElement("option");
                        option.value = value;
                        option.innerHTML = value;
                        selectEle.options.add(option);
                    });
                    $("#inputFilterComputers").val("");
                    $("#divSearchComputers > div").slideDown();
                    break;
                case "Bounds":
                    context.canvas.height = jsonMessage.Height;
                    context.canvas.width = jsonMessage.Width;
                    break;
                case "RTCCandidate":
                    rtcConnection.addIceCandidate(new RTCIceCandidate(jsonMessage.Candidate));
                    break;
                case "RTCOffer":
                    if (jsonMessage.Status = "Denied") {
                        var request = {
                            "Type": "CaptureScreen",
                            "Source": "WebSocket"
                        };
                        console.log("RTC unsupported by client.  Falling back to websocket communication.");
                        socket.send(JSON.stringify(request));
                    }
                    break;
                case "RTCAnswer":
                    var answer = JSON.parse(atob(jsonMessage.Answer));
                    rtcConnection.setRemoteDescription(answer, function () {
                        var request = {
                            "Type": "CaptureScreen",
                        };
                        console.log("RTC descriptions set successfully.  Requesting video via RTC.");
                        socket.send(JSON.stringify(request));
                    }, function (error) {
                        var request = {
                            "Type": "CaptureScreen",
                            "Source": "WebSocket"
                        };
                        console.log("RTC connection failed.  Falling back to websocket communication.");
                        socket.send(JSON.stringify(request));
                    });
                default:
                    break;
            }
            ;
        }
    };
}
function disconnect() {
    socket.close();
}
function initRTC() {
    rtcConnection = new RTCPeerConnection({
        iceServers: [
            {
                urls: [
                    "stun:play.after-game.net",
                    "stun:stun.stunprotocol.org",
                    "stun:stun.l.google.com:19302",
                    "stun:stun1.l.google.com:19302",
                    "stun:stun2.l.google.com:19302",
                    "stun:stun3.l.google.com:19302",
                    "stun:stun4.l.google.com:19302"
                ]
            }
        ]
    });
    rtcConnection.onicecandidate = function (evt) {
        if (evt.candidate) {
            socket.send(JSON.stringify({
                'Type': 'RTCCandidate',
                'Candidate': evt.candidate
            }));
        }
    };
    rtcConnection.ontrack = function (evt) {
        $("#videoRemoteControl").show();
        $("#videoRemoteControl")[0].src = URL.createObjectURL(evt.streams[0]);
    };
    rtcConnection.createOffer(function (offer) {
        // Success callback.
        rtcConnection.setLocalDescription(offer, function () {
            // Success callback.
            var request = {
                "Type": "RTCOffer",
                "Offer": btoa(JSON.stringify(rtcConnection.localDescription)),
            };
            socket.send(JSON.stringify(request));
        }, function (error) {
            // Failure callback.
            var request = {
                "Type": "CaptureScreen",
                "Source": "WebSocket",
            };
            console.log("RTC connection failed.  Falling back to websocket communication.");
            socket.send(JSON.stringify(request));
        });
    }, function (error) {
        // Failure callback.
        var request = {
            "Type": "CaptureScreen",
            "Source": "WebSocket",
        };
        console.log("RTC connection failed.  Falling back to websocket communication.");
        socket.send(JSON.stringify(request));
    }, { 'offerToReceiveVideo': true });
}
function sendRefreshRequest() {
    var request = {
        "Type": "RefreshScreen"
    };
    context.canvas.width = 300;
    context.canvas.height = 300;
    socket.send(JSON.stringify(request));
}
function toggleMenu() {
    $("#divMenu").slideToggle();
    $("#imgMenu").toggleClass("rotated180");
}
function toggleScaleToFit() {
    if ($("#divScaleToFit").attr("status") == "off") {
        $("#divScaleToFit").attr("status", "on");
        $(".input-surface").css("width", "100%");
    }
    else {
        $("#divScaleToFit").attr("status", "off");
        $(".input-surface").css("width", "auto");
    }
}
function sendClipboard(e) {
    // Do nothing if input is empty.
    if ($("#textClipboard").val() == "") {
        return;
    }
    if (socket.readyState != socket.OPEN) {
        return;
    }
    var request = {
        "Type": "SendClipboard",
        "Data": btoa(e.target.value),
    };
    socket.send(JSON.stringify(request));
    showTooltip(e.target, "bottom", "black", "Clipboard data sent.");
    $("#textClipboard").val("");
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
        "font-size": ".8em"
    });
    var rectPlacement = objPlacementTarget.getBoundingClientRect();
    switch (strPlacementDirection) {
        case "top":
            {
                divTooltip.style.top = Number(rectPlacement.top - 5) + "px";
                divTooltip.style.transform = "translateY(-100%)";
                divTooltip.style.left = rectPlacement.left + "px";
                break;
            }
        case "right":
            {
                divTooltip.style.top = rectPlacement.top + "px";
                divTooltip.style.left = Number(rectPlacement.right + 5) + "px";
                break;
            }
        case "bottom":
            {
                divTooltip.style.top = Number(rectPlacement.bottom + 5) + "px";
                divTooltip.style.left = rectPlacement.left + "px";
                break;
            }
        case "left":
            {
                divTooltip.style.top = rectPlacement.top + "px";
                divTooltip.style.left = Number(rectPlacement.left - 5) + "px";
                divTooltip.style.transform = "translateX(-100%)";
                break;
            }
        case "center":
            {
                divTooltip.style.top = Number(rectPlacement.bottom - (rectPlacement.height / 2)) + "px";
                divTooltip.style.left = Number(rectPlacement.right - (rectPlacement.width / 2)) + "px";
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
function transferFiles(fileList) {
    if (socket.readyState != socket.OPEN) {
        return;
    }
    $.each(fileList, function (index, file) {
        var strPath = (location.pathname.toLowerCase().split("/remote_control")[0] + "/Services/FileTransfer.cshtml").replace("//", "/");
        fr.onload = function () {
            var upload = JSON.stringify({
                "Type": "Upload",
                "File": fr.result
            });
            $.post(strPath, upload, function (data) {
                var request = {
                    "Type": "FileTransfer",
                    "FileName": file.name,
                    "RetrievalCode": data,
                };
                socket.send(JSON.stringify(request));
            });
        };
        fr.readAsDataURL(file);
    });
}
$(document).ready(function () {
    context = document.getElementById("canvasRemoteControl").getContext("2d");
    $(".toggle-box").on("click", function (e) {
        if (e.currentTarget.id == "divInteractive") {
            document.getElementById("inputSessionID").setAttribute("placeholder", "Enter client's session ID.");
            document.getElementById("inputSessionID").setAttribute("pattern", "[0-9 ]*");
            $("#imgSearchComputers").hide();
        }
        else
        {
            if (InstaTech.UserID == undefined || InstaTech.AuthenticationToken == undefined)
            {
                showDialog("Login Required", "You must be logged in to start an unattended access sessions.");
                return;
            }
            $("#imgSearchComputers").show();
            document.getElementById("inputSessionID").setAttribute("placeholder", "Enter client's computer name.");
            document.getElementById("inputSessionID").removeAttribute("pattern");
        }
        $(".toggle-box").removeClass("selected");
        e.currentTarget.classList.add("selected");
    });
    $(".input-surface").on("mousemove", function (e) {
        e.preventDefault();
        if (socket.readyState == WebSocket.OPEN) {
            var pointX = e.offsetX / $(e.target).width();
            var pointY = e.offsetY / $(e.target).height();
            var request = {
                "Type": "MouseMove",
                "PointX": pointX,
                "PointY": pointY
            };
            socket.send(JSON.stringify(request));
        }
    });
    $(".input-surface").on("mousedown", function (e) {
        if (socket.readyState == WebSocket.OPEN) {
            if (e.button != 0 && e.button != 2) {
                return;
            }
            var pointX = e.offsetX / $(e.target).width();
            var pointY = e.offsetY / $(e.target).height();
            var request = {
                "Type": "MouseDown",
                "PointX": pointX,
                "PointY": pointY
            };
            if (e.button == 0) {
                request.Button = "Left";
            }
            else if (e.button == 2) {
                request.Button = "Right";
            }
            socket.send(JSON.stringify(request));
        }
    });
    $(".input-surface").on("mouseup", function (e) {
        if (socket.readyState == WebSocket.OPEN) {
            if (e.button != 0 && e.button != 2) {
                return;
            }
            var pointX = e.offsetX / $(e.target).width();
            var pointY = e.offsetY / $(e.target).height();
            var request = {
                "Type": "MouseUp",
                "PointX": pointX,
                "PointY": pointY
            };
            if (e.button == 0) {
                request.Button = "Left";
            }
            else if (e.button == 2) {
                e.preventDefault();
                request.Button = "Right";
            }
            socket.send(JSON.stringify(request));
        }
    });
    $(".input-surface").on("touchstart", function (e) {
        if (socket.readyState != WebSocket.OPEN) {
            return;
        }
        currentTouches++;
        var touchPointOffset = e.target.getBoundingClientRect();
        lastTouchPointX = (e.touches[0].clientX - touchPointOffset.left) / $(e.target).width();
        lastTouchPointY = (e.touches[0].clientY - touchPointOffset.top) / $(e.target).height();
        if (e.touches.length > 1) {
            multiTouched = true;
            lastMultiTouch = Date.now();
        }
        if (e.touches.length == 1) {
            e.preventDefault();
            if (Date.now() - lastTouch < 500) {
                doubleTapped = true;
                window.setTimeout(function () {
                    if (currentTouches == 1 && doubleTapped && !multiTouched) {
                        var request = {
                            "Type": "TouchDown",
                        };
                        socket.send(JSON.stringify(request));
                        touchDragging = true;
                    }
                }, 500);
            }
            else {
                window.setTimeout(function () {
                    if (currentTouches == 1 && !doubleTapped && !multiTouched && !cancelNextTouch && !touchDragging) {
                        var request = {
                            "Type": "LongPress",
                            "Button": "Right"
                        };
                        socket.send(JSON.stringify(request));
                    }
                }, 500);
            }
        }
        ;
        lastTouch = Date.now();
    });
    $(".input-surface").on("touchmove", function (e) {
        if (socket.readyState != WebSocket.OPEN) {
            return;
        }
        cancelNextTouch = true;
        if (multiTouched && !touchDragging) {
            return;
        }
        if (e.touches.length == 1) {
            e.preventDefault();
            var touchPointOffset = e.target.getBoundingClientRect();
            var pointX = (e.touches[0].clientX - touchPointOffset.left) / $(e.target).width();
            var pointY = (e.touches[0].clientY - touchPointOffset.top) / $(e.target).height();
            var request = {
                "Type": "TouchMove",
                "MoveByX": pointX - lastTouchPointX,
                "MoveByY": pointY - lastTouchPointY,
            };
            socket.send(JSON.stringify(request));
            lastTouchPointX = pointX;
            lastTouchPointY = pointY;
        }
    });
    $(".input-surface").on("touchend", function (e) {
        if (socket.readyState == WebSocket.OPEN) {
            e.preventDefault();
        }
        else {
            return;
        }
        currentTouches--;
        var request;
        var touchPointOffset = e.target.getBoundingClientRect();
        var pointX = (e.changedTouches[0].clientX - touchPointOffset.left) / $(e.target).width();
        var pointY = (e.changedTouches[0].clientY - touchPointOffset.top) / $(e.target).height();
        if (e.touches.length == 0) {
            e.preventDefault();
            if (cancelNextTouch || multiTouched || touchDragging) {
                if (touchDragging) {
                    touchDragging = false;
                    request = {
                        "Type": "TouchUp",
                    };
                    socket.send(JSON.stringify(request));
                }
            }
            else {
                request = {
                    "Type": "Tap",
                };
                socket.send(JSON.stringify(request));
            }
            cancelNextTouch = false;
            multiTouched = false;
            doubleTapped = false;
        }
    });
    $(window).on("keydown", function (e) {
        if ($(":focus").length == 0 && socket.readyState == WebSocket.OPEN) {
            e.preventDefault();
            if (e.key == "Alt" || e.key == "Shift" || e.key == "Ctrl") {
                return;
            }
            var key = e.key;
            if (e.altKey) {
                key = "%" + key;
            }
            else if (e.ctrlKey) {
                key = "^" + key;
            }
            else if (e.shiftKey) {
                key = "+" + key;
            }
            var request = {
                "Type": "KeyPress",
                "Key": key,
            };
            socket.send(JSON.stringify(request));
        }
        ;
    });
    $(".input-surface").on("click", function (e) {
        $(":focus").blur();
    });
    $(window).on("click", function (e) {
        if (e.button == 2 && $(":focus").length == 0) {
            e.preventDefault();
        }
        if ($("#divMenu").is(":visible") && !$("#imgMenu").is(":hover") && !$("#divMenu").is(":hover")) {
            toggleMenu();
        }
        ;
    });
    $(window).on("touchstart", function (event) {
        if (event.touches.length > 2) {
            if (document.documentElement.requestFullscreen) {
                document.documentElement.requestFullscreen();
            }
            else if (document.documentElement.webkitRequestFullscreen) {
                document.documentElement.webkitRequestFullscreen();
            }
            else if (document.documentElement.mozRequestFullScreen) {
                document.documentElement.mozRequestFullScreen();
            }
            ;
        }
        ;
    });
    window.ondragover = function (e) {
        e.preventDefault();
        e.dataTransfer.dropEffect = "copy";
    };
    window.ondrop = function (e) {
        e.preventDefault();
        if (e.dataTransfer.files.length < 1) {
            return;
        }
        transferFiles(e.dataTransfer.files);
        showTooltip($(document.body), "center", "seagreen", "File(s) uploaded successfully.");
    };
    initWebSocket();
});
