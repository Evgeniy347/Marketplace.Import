
function MPS_Init() {

    if (!window.MPS_Context)
        return;

    if (!window.MPS_Context.StartAuthorization) {
        window.MPS_Context.StartAuthorization = true;
        MPS_SaveContext();
        MPS_GetToken();
    }
}

function MPS_GetToken() {

    var login = "alisa-kot96@mail.ru";
    var url = "https://partner.lamoda.ru/api/token";
    var password = "{Password:alisa-kot96@mail.ru}";
    var request = {
        "grant_type": "password", "username": login, "password": password
    }

    var builder = MPS_CreateRestBuilder();
    builder.SendPost(url, request, MPS_GetTokenCallBack);
}


function MPS_GetTokenCallBack(responce) {

    var url = "https://partner.lamoda.ru/api/v1/exports";
    var endDate = new Date();
    var startDate = endDate.addDays(-40); // 40 дней от текущей даты 
    var filterDate = MPS_DateLamodaFormat(startDate) + "%2C" + MPS_DateLamodaFormat(endDate, true);
    var request = {
        "filter": "createdAt%26gt%3B%3D%26lt%3B" + filterDate,
        "type": "orders_and_items",
        "format": "csv",
        "sort": "-createdAt",
        "description": null
    };

    var builder = MPS_CreateRestBuilder();
    builder.SuccessStatus.push(202);

    builder.AddRequestHeader("Authorization", "Bearer " + responce.access_token);
    builder.SendPost(url, request, MPS_CreateExportCallback, responce.access_token);
}

function MPS_DateLamodaFormat(d, end) {
    //2022 09 01 (000000 or 235959)
    var datestring = d.getFullYear() + ("0" + (d.getMonth() + 1)).slice(-2) + ("0" + d.getDate()).slice(-2);
    datestring += end ? "235959" : "000000";
    return datestring;
}

function MPS_CreateExportCallback(responce, access_token) {

    var url = "https://partner.lamoda.ru/api/v1/exports/" + responce.id + "/download";

    var params = {
        access_token: access_token,
        FileURL: url,
    };
    MPS_DownloadExport(params);
}

function MPS_DownloadExport(params) {

    var builder = MPS_CreateGetBuilder();

    builder.AddRequestHeader("Authorization", "Bearer " + params.access_token);
    builder.SuccessStatus.push(404);
    builder.SendPost(params.FileURL, MPS_DownloadExportCallback, params);
}

function MPS_DownloadExportCallback(responce, params) {

    if (!responce || responce.byteLength == 0) {
        setTimeout(MPS_DownloadExport, 1000, params);
        return;
    }

    MPS_DownloadData(responce, "report.csv", "application/octet-stream");
}

function MPS_CreateGetBuilder() {
    var request = new XMLHttpRequest();
    request.MPS_RequestHeaders = [];

    request.AddRequestHeader = function (key, value) {
        request.MPS_RequestHeaders.push({ Key: key, Value: value });
    }

    request.SuccessStatus = [200];

    request.SendPost = function (url, callback, arg) {
        this.open("GET", url, true);
        this.responseType = "arraybuffer";

        for (var i = 0; i < this.MPS_RequestHeaders.length; i++)
            this.setRequestHeader(this.MPS_RequestHeaders[i].Key, this.MPS_RequestHeaders[i].Value);

        this.mps_callback = callback;
        this.mps_callback_arg = arg;
        this.addEventListener("readystatechange", () => {

            if (this.readyState === 4 && this.SuccessStatus.indexOf(this.status) != -1) {
                if (this.mps_callback)
                    this.mps_callback(request.response, request.mps_callback_arg);
            }
        });
        this.send();
    }
    return request;
}

function MPS_DownloadData(data, filename, type) {
    var file = new Blob([data], { type: type });
    if (window.navigator.msSaveOrOpenBlob) // IE10+
        window.navigator.msSaveOrOpenBlob(file, filename);
    else { // Others
        var a = document.createElement("a"),
            url = URL.createObjectURL(file);
        a.href = url;
        a.download = filename;
        document.body.appendChild(a);
        a.click();
        setTimeout(function () {
            document.body.removeChild(a);
            window.URL.revokeObjectURL(url);
        }, 0);
    }
}

MPS_Init();