# Đo từng request LLM

`POST /api/llm/complete` nhận `{"prompt":"Tóm tắt quy trình nhập kho"}`.
Adapter gửi request không streaming theo hợp đồng JSON chat completions:
`model`, `messages`, `stream: false`; response cần `choices[0].message.content`.
Không có lời gọi LLM nào trước thay đổi này. Endpoint mới là điểm tích hợp mẫu,
không tự động gửi dữ liệu đơn hàng/kho cho nhà cung cấp.

## Cấu hình

Thiết lập bằng environment variables hoặc user secrets; không commit API key:

```text
Llm__Endpoint=https://your-provider.example/v1/chat/completions
Llm__ApiKey=<secret>
Llm__Model=<exact-model-name>
Llm__TimeoutSeconds=60
Llm__Prices__<exact-model-name>__InputPerMillion=2
Llm__Prices__<exact-model-name>__OutputPerMillion=8
Llm__Prices__<exact-model-name>__CachedInputPerMillion=1
```

Các giá trên chỉ là số minh họa, không phải bảng giá nhà cung cấp.
Cập nhật giá USD/1 triệu token theo hợp đồng của bạn, dùng đúng tên model trong
response (kể cả tên snapshot). Không tự suy đoán giá từ tên alias.
Bổ sung authentication, authorization và quota phù hợp trước khi mở endpoint
này ra Internet; endpoint hiện theo cơ chế truy cập của các API sẵn có trong repo.

## Kết quả và log

Response gồm `text` và `measurement`: `requestId`, `model`, `latencyMs`, `status`,
`usage` (input/output/cached input/total tokens), `estimatedCostUsd`.
Mỗi lần gọi provider ghi một structured log `LLM request ...`, liên kết response
bằng requestId. Có thể cấu hình logging JSON của ASP.NET để thu thập các trường.
Không ghi prompt, completion, API key hoặc nội dung lỗi provider.
Log là nơi xem cả request thành công và thất bại; không có kho lịch sử trong RAM.

Latency dùng Stopwatch, từ trước khi gửi đến khi đọc và phân tích toàn bộ response.
Không phải time-to-first-token hay latency HTTP toàn tuyến. Token lấy từ
`usage.prompt_tokens`, `usage.completion_tokens` và
`usage.prompt_tokens_details.cached_tokens`, không đếm bằng số từ.

Chi phí ước tính = ((input − cached) × giá input + cached × giá cached +
output × giá output) / 1.000.000. Cached tokens nằm trong input, không cộng hai lần.
Usage thiếu/sai → usage và cost null. Giá thiếu/âm hoặc model chưa khai báo → cost null.
Không có cached tokens thì không bắt buộc cấu hình giá cached.
Đây là ước tính theo token, không bao gồm thuế, phí công cụ hoặc điều chỉnh hóa đơn.

Provider HTTP error → 502, timeout → 504, thiếu cấu hình → 503.
Log status gồm `success`, `error`, `timeout`, `cancelled`. Lỗi/timeout không có usage
thì cost null, vì không thể kết luận nhà cung cấp không tính tiền.
Chưa có retry: mỗi lần gọi tương ứng một attempt. Request bị validation từ chối
trước khi gọi provider không tạo usage log.

## Kiểm thử

```bash
dotnet test HelloWorldMvc.sln --configuration Release
```

`LlmMeasurementTests` dùng HTTP handler giả, không cần API key thật: kiểm tra công
thức chi phí, cached tokens, usage sai/thiếu, response thành công, lỗi HTTP,
cancellation và tránh lộ nội dung trong log. Workflow .NET hiện có chạy các test
này trên pull request.
