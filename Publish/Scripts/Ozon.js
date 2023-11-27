
function MPS_Init() {
    console.log("MPS_Init");

    if (!window.MPS_Context) {
        console.log("Close: not MPS_Context");
        return;
    }

    if (location.href.startsWith("https://seller.ozon.ru/app/registration/signin")) {
        MPS_PushLog("StartAuth");
        console.log("EnableBrowser");
    } else if (location.href.startsWith("https://seller.ozon.ru/app/analytics/fulfillment-reports/operation-orders-fbo")) {
        if (!window.document.body.StartCreateExport) {
            window.document.body.StartCreateExport = true;
            console.log("DisableBrowser");

            setTimeout(function () { MPS_CreateExport(); }, 1000);
        } else {
            console.log("Close: Exist StartCreateExport");
        }
    } else {
        console.log("setTimeout MPS_Init");
        setTimeout(MPS_Init, 1000);
    }
}

function MPS_CreateExport() {
    setTimeout(function () { MPS_CreateExportReport() }, 400);
    setTimeout(function () { MPS_GetGraphs() }, 600);
}

function MPS_CreateExportReport() {
    MPS_PushLog("MPS_CreateConsolidatedExport");

    var url = "https://seller.ozon.ru/api/site/report-service/v2/report/company/postings";

    var dateTo = new Date();
    var dateFrom = dateTo.addDays(-40); // 40 дней от текущей даты

    var params = {
        "filter": {
            "processed_at_from": dateFrom.toStringMPS("yyyy-MM-ddT00:00:00Z"),
            "processed_at_to": dateTo.toStringMPS("yyyy-MM-ddT23:59:59Z"),
            "delivery_schema": "fbo"
        },
        "lang": "RU",
        "with": { "analytics_data": true, "jewelry_codes": true },
        "company_id": MPS_GetCookie("contentId"),
        "sort_dir": "desc"
    };

    var request = MPS_CreateRestBuilder();
    request.SendPost(url, params, MPS_CreateExportReportCallback);
}

function MPS_CreateExportReportCallback(responce) {
    MPS_PushLog("MPS_CreateExportReportCallback");
    setTimeout(MPS_ReportStatus, 3000, { code: responce.code, callback: MPS_GetList, info: "Report" });
}

function MPS_ReportStatus(arg) {

    MPS_PushLog("MPS_ReportStatus " + arg.info);

    var url = "https://seller.ozon.ru/api/site/report-service/report/status";
    var request = MPS_CreateRestBuilder();

    request.AddRequestHeader("x-o3-company-id", MPS_GetCookie("contentId"));
    request.AddRequestHeader("x-o3-language", "ru");

    request.SendPost(url, { code: arg.code }, MPS_ReportStatusCallback, arg);
}

function MPS_ReportStatusCallback(parameters, arg) {
    MPS_PushLog("MPS_ReportStatusCallback");

    if (parameters.status == "failed") {
        console.log("status failed");
        console.log("StopAppScript");
    }

    if ((parameters.status == "processing" ||
        parameters.status == "waiting") &&
        parameters.error_code == 0) {
        setTimeout(MPS_ReportStatus, 3000, arg);
    } else {
        setTimeout(function () { arg.callback(arg.code) }, 2000);
    }
}

function MPS_GetList(code) {
    MPS_PushLog("MPS_GetList");
    var url = "https://seller.ozon.ru/api/site/report-service/report/list";

    var params = {
        "filter": { "code": [code] }
    };

    var request = MPS_CreateRestBuilder();
    request.AddRequestHeader("x-o3-company-id", MPS_GetCookie("contentId"));
    request.AddRequestHeader("x-o3-language", "ru");

    request.SendPost(url, params, MPS_GetListCallback);
}

function MPS_GetListCallback(responce) {
    MPS_PushLog("MPS_GetListCallback");

    var fileSourceUrl = new URL(responce.result[0].file);
    var path = MPS_TrimChar(fileSourceUrl.pathname, "/");
    console.log("source file url:" + fileSourceUrl);

    var fileUrl = "https://seller.ozon.ru/api/site/storage/" + btoa(path);
    console.log("full file url:" + fileUrl);

    var builder = MPS_CreateGetBuilder();
    builder.AddRequestHeader("x-o3-company-id", MPS_GetCookie("contentId"));
    builder.AddRequestHeader("x-o3-language", "ru");
    builder.AddRequestHeader("accept", "application/json, text/plain, */*");
    builder.AddRequestHeader("accept-language", "ru");
    builder.ResponseType = "text";

    setTimeout(function () { builder.SendPost(fileUrl, MPS_DownloadFileCallback) }, 600);
}

function MPS_GetGraphs() {

    MPS_PushLog("MPS_GetGraphs");
    var url = "https://seller.ozon.ru/api/v1/report/data-v1-xlsx";

    var dateTo = new Date();
    var dateFrom = dateTo.addDays(-40); // 40 дней от текущей даты

    var params = {
        "filters": [],
        "metrics": ["ordered_units", "session_view", "session_view_pdp", "conv_tocart_pdp", "revenue", "cancellations", "returns", "position_category"],
        "dimensions": ["brand", "category3", "sku", "day", "modelID"],
        "date_from": dateFrom.toStringMPS("yyyy-MM-dd"),
        "date_to": dateTo.toStringMPS("yyyy-MM-dd"),
        "is_action": false
    };

    var request = MPS_CreateRestBuilder();
    request.AddRequestHeader("x-o3-company-id", MPS_GetCookie("contentId"));
    request.AddRequestHeader("x-o3-language", "ru");
    request.AddRequestHeader("x-o3-app-name", "seller-ui");
    request.AddRequestHeader("x-o3-page-type", "analytics-other");

    request.SendPost(url, params, MPS_CreateExportGraphsCallback);
}

function MPS_CreateExportGraphsCallback(responce) {
    MPS_PushLog("MPS_CreateExportGraphsCallback");
    setTimeout(MPS_ReportStatusV1, 2600, { code: responce.code, callback: MPS_GetGraphsDownload, info: "Graphs" });
}

function MPS_ReportStatusV1(arg) {

    MPS_PushLog("MPS_ReportStatusV1 " + arg.info);

    var url = "https://seller.ozon.ru/api/v1/report/status/" + arg.code;
    var request = MPS_CreateGetBuilder();

    request.AddRequestHeader("x-o3-company-id", MPS_GetCookie("contentId"));
    request.AddRequestHeader("x-o3-language", "ru");
    request.AddRequestHeader("x-o3-app-name", "seller-ui");
    request.AddRequestHeader("x-o3-page-type", "analytics-other");
    request.AddRequestHeader("accept", "application/json, text/plain, */*");
    request.AddRequestHeader("accept-language", "ru");

    request.SendPost(url, MPS_ReportStatusV1Callback, arg);
}
 
function MPS_ReportStatusV1Callback(parameters, arg) {
    MPS_PushLog("MPS_ReportStatusV1Callback");

    if (parameters.status == "failed") {
        console.log("status failed");
        console.log("StopAppScript");
    }

    if ((parameters.status == "processing" ||
        parameters.status == "waiting") &&
        parameters.error_code == 0) {
        setTimeout(MPS_ReportStatusV1, 3000, arg);
    } else {
        setTimeout(function () { arg.callback(arg.code) }, 2000);
    }
}
 
function MPS_ReportDownloadGraphV1(parameters, arg) {
    MPS_PushLog("MPS_ReportStatusV1Callback");
    var responceText = String.fromCharCode.apply(null, new Uint8Array(array));
    console.log(responceText);
    var parameters = JSON.parse(responceText);

    if (parameters.status == "failed") {
        console.log("status failed");
        console.log("StopAppScript");
    }

    if ((parameters.status == "processing" ||
        parameters.status == "waiting") &&
        parameters.error_code == 0) {
        setTimeout(MPS_ReportStatusV1, 3000, arg);
    }
    else {
        MPS_GetGraphsDownload(code);
    }
}

function MPS_GetGraphsDownload(code) {
    MPS_PushLog("MPS_GetGraphsDownload");

    var fileUrl = "https://seller.ozon.ru/api/v1/report/download/" + code;
    console.log("full file url:" + fileUrl);

    var builder = MPS_CreateGetBuilder();
    builder.AddRequestHeader("x-o3-company-id", MPS_GetCookie("contentId"));
    builder.AddRequestHeader("x-o3-language", "ru");
    builder.AddRequestHeader("x-o3-app-name", "seller-ui");
    builder.AddRequestHeader("x-o3-page-type", "analytics-other");
    builder.AddRequestHeader("accept", "application/json, text/plain, */*");
    builder.AddRequestHeader("accept-language", "ru");
    builder.ResponseType = 'arraybuffer';

    builder.SendPost(fileUrl, MPS_GetGraphsDownloadCallback);
}

function MPS_GetGraphsDownloadCallback(responce) {
    MPS_PushLog("MPS_GetGraphsDownloadCallback");
    MPS_DownloadData(responce, "graphs.xlsx", "application/octet-stream");
    MPS_UpdateLog(function (x) { x.DownloadGraphs = true; });
    MPS_SaveContext();

    MPS_CheckStopScript();
}

function MPS_DownloadFileCallback(responce) {
    MPS_PushLog("MPS_DownloadFileCallback");
    MPS_DownloadData(responce, "report.csv", "application/octet-stream");
    MPS_UpdateLog(function (x) { x.DownloadList = true; });

    MPS_CheckStopScript();
}

function MPS_CheckStopScript() {
    if (window.MPS_Context.DownloadList && window.MPS_Context.DownloadGraphs)
        console.log("StopAppScript");
}

MPS_Init();