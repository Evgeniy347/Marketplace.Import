
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
    MPS_CreateExportReport();
    MPS_GetGraphs();
}

function MPS_CreateExportReport() {
    MPS_PushLog("MPS_CreateConsolidatedExport");

    var url = "https://seller.ozon.ru/api/site/report-service/v2/report/company/postings";

    var dateTo = new Date();
    var dateFrom = dateTo.addDays(-40); // 40 дней от текущей даты

    //var params = {
    //    "company_id": MPS_GetCookie("contentId"),
    //    "processed_at_from": dateFrom.toStringMPS("yyyy-MM-ddT00:00:00Z"),
    //    "processed_at_to": dateTo.toStringMPS("yyyy-MM-ddT23:59:59Z"),
    //    "status_alias": ["awaiting_packaging", "awaiting_deliver", "delivering", "delivered", "cancelled"]
    //};

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
    MPS_ReportStatus(responce.code);
}

function MPS_ReportStatus(code) {
    MPS_PushLog("MPS_ReportStatus");

    var url = "https://seller.ozon.ru/api/site/report-service/report/status";

    var request = MPS_CreateRestBuilder();

    request.AddRequestHeader("x-o3-company-id", MPS_GetCookie("contentId"));
    request.AddRequestHeader("x-o3-language", "ru");

    request.SendPost(url, { code: code }, MPS_ReportStatusCallback, code);
}

function MPS_ReportStatusCallback(parameters, code) {
    MPS_PushLog("MPS_ReportStatusCallback");

    if (parameters.status == "processing" && parameters.error_code == 0) {
        setTimeout(MPS_ReportStatus, 1000, code);
    } else {
        MPS_GetList(code);
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


function MPS_GetGraphs() {
    MPS_PushLog("MPS_GetGraphs");
    var url = "https://seller.ozon.ru/api/site/seller-analytics/data/v1/xlsx";

    var params = {
        "filters": [],
        "metrics": ["ordered_units", "session_view", "session_view_pdp", "conv_tocart_pdp", "revenue", "cancellations", "returns", "position_category"],
        "dimensions": ["category3", "sku", "day", "modelID"],
        "date_from": "2022-10-14",
        "date_to": "2022-11-13",
        "is_action": false
    };

    var request = MPS_CreateRestBuilder();
    request.ResponseType = 'arraybuffer';
    request.AddRequestHeader("x-o3-company-id", MPS_GetCookie("contentId"));
    request.AddRequestHeader("x-o3-language", "ru");

    request.SendPost(url, params, MPS_GetGraphsCallback);
}


function MPS_GetGraphsCallback(responce) {

    MPS_PushLog("MPS_DownloadFileCallback");
    MPS_DownloadData(responce, "graphs.xlsx", "application/octet-stream");
    window.MPS_Context.DownloadGraphs = true;

    MPS_CheckStopScript();
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

    builder.SendPost(fileUrl, MPS_DonloadFileCallback);
}

function MPS_DonloadFileCallback(responce) {
    MPS_PushLog("MPS_DownloadFileCallback");
    MPS_DownloadData(responce, "report.csv", "application/octet-stream");
    window.MPS_Context.DownloadList = true;

    MPS_CheckStopScript();
}

function MPS_CheckStopScript() {
    if (window.MPS_Context.DownloadList && window.MPS_Context.DownloadGraphs)
        console.log("StopAppScript");
}

MPS_Init();