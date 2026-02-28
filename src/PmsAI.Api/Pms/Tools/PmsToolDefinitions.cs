namespace PmsAI.Api.Pms.Tools;

public static class PmsToolDefinitions
{
    public static List<object> GetAllTools() => new()
    {
        new
        {
            type = "function",
            function = new
            {
                name = "pms_get_reservations",
                description = "查询酒店预订或在住信息。需要至少提供一个查询条件（reservationNo/guestName/phone/arrivalDate）。若用户有多个酒店权限且未指定酒店，必须先追问用户。",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        hotelRef = new { type = "string", description = "酒店名称、别名或编号（可选，服务端解析为GUID）" },
                        reservationNo = new { type = "string", description = "预订单号（优先使用）" },
                        guestName = new { type = "string", description = "宾客姓名（模糊匹配）" },
                        phone = new { type = "string", description = "宾客手机号" },
                        arrivalDate = new { type = "string", description = "抵达日期，格式YYYY-MM-DD" }
                    },
                    required = Array.Empty<string>()
                }
            }
        },
        new
        {
            type = "function",
            function = new
            {
                name = "pms_get_room_status",
                description = "查询酒店房态（空房/在住/预抵/维修等）",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        hotelRef = new { type = "string", description = "酒店名称、别名或编号" },
                        date = new { type = "string", description = "查询日期，格式YYYY-MM-DD，默认当天" }
                    },
                    required = Array.Empty<string>()
                }
            }
        },
        new
        {
            type = "function",
            function = new
            {
                name = "pms_create_work_order",
                description = "为指定房间创建工单（维修/客房/IT/服务等）。此操作需要用户确认后才执行。",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        hotelRef = new { type = "string" },
                        roomNo = new { type = "string", description = "房间号" },
                        category = new { type = "string", @enum = new[] { "Housekeeping", "Maintenance", "IT", "Service", "Other" } },
                        priority = new { type = "string", @enum = new[] { "Low", "Normal", "High", "Urgent" } },
                        description = new { type = "string", description = "工单描述，最多500字" }
                    },
                    required = new[] { "roomNo", "category", "priority", "description" }
                }
            }
        },
        new
        {
            type = "function",
            function = new
            {
                name = "pms_request_late_checkout",
                description = "申请延迟退房。此操作需要用户确认后才执行。",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        hotelRef = new { type = "string" },
                        reservationNo = new { type = "string", description = "预订单号" },
                        requestedCheckoutTime = new { type = "string", description = "申请退房时间，ISO 8601格式含时区，如2026-02-26T14:00:00+08:00" },
                        reason = new { type = "string", description = "延退原因（可选）" }
                    },
                    required = new[] { "reservationNo", "requestedCheckoutTime" }
                }
            }
        }
    };

    public static readonly HashSet<string> WritableTools = new()
    {
        "pms_create_work_order",
        "pms_request_late_checkout"
    };

    public static readonly HashSet<string> ReadableTools = new()
    {
        "pms_get_reservations",
        "pms_get_room_status"
    };
}
