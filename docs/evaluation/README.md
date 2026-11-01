# Đánh giá retrieval quality và answer faithfulness

MiCo hiện có `/api/llm/complete` và đo chi phí/latency; chưa có retriever RAG.
Module này đánh giá **trace đã thu thập**, không tạo ra hay chạy retriever giả.
Không gọi provider, không tốn token và không gửi dữ liệu ra ngoài.

## Chạy thử

Khởi động backend theo hướng dẫn setup của repo, sau đó:

```bash
curl -X POST http://localhost:5000/api/llm/evaluations \
  -H 'Content-Type: application/json' \
  --data-binary @docs/evaluation/order-flow.json
dotnet test HelloWorldMvc.sln --configuration Release
```

Thay port theo URL backend thực tế. Fixture dùng nội dung `docs/business/order-flow.md`
ở commit `a13ea80f8fe2453182e87a1a3fa0b7a8c682da0f`; answer là mẫu cố ý thêm một
nhận định không có nguồn. Đây không phải kết quả chất lượng thực tế của MiCo.
Kết quả mẫu: Precision@2 = 0.5, Recall@2 = 1, MRR@2 = 1, nDCG@2 = 1,
faithfulness = 0.5 và reviewCoverage = 1.

## Hợp đồng đánh giá

- `retrieved`: chunk ID và nội dung, theo đúng thứ tự ranking. Chỉ top K là context
  đã đưa cho generator; nếu pipeline lọc/rerank thì ghi trace sau bước đó.
- `relevance`: nhãn gold theo chunk ID, 0 (không liên quan) đến 3 (rất liên quan).
  Nhãn 1..3 là relevant trong precision/recall/MRR. ID không có nhãn tính là 0.
  Phải gán nhãn đủ ứng viên và cả relevant chunk bị bỏ lỡ để recall có ý nghĩa.
- Precision@k = số relevant trong top k / k; thiếu kết quả vẫn chia k.
- Recall@k = số relevant trong top k / tổng relevant gold.
- MRR@k: nghịch đảo hạng relevant đầu tiên, 0 nếu không tìm thấy.
- nDCG@k: DCG/ideal DCG với gain `2^grade - 1`, discount `log2(rank + 1)`.
  Gold không có relevant: recall/MRR/nDCG trả null; không xem là đạt hoàn hảo.
  Ranking chứa ID trùng bị từ chối để tránh tăng điểm giả.
- `claims`: reviewer tách **toàn bộ** nhận định thực tế của answer thành các span
  nguyên văn, gán `supported`, `unsupported`, `contradicted`, hoặc `unreviewed`.
  `supported` và `contradicted` cần quote nguyên văn từ chunk trong top k.
- Faithfulness = supported / (supported + unsupported + contradicted).
  Không có claim đã review thì score null. ReviewCoverage = số claim đã review /
  số claim gửi lên, bằng 0 khi không có claim. Không công bố score là đạt nếu coverage
  chưa đủ; coverage không phát hiện được reviewer đã bỏ sót claim trong answer.

Đây là **human-annotated-v1**, không phải semantic judge tự động. Kiểm tra quote
chỉ bảo đảm bằng chứng tồn tại, không chứng minh entailment: reviewer chịu trách nhiệm
phát hiện phủ định, sai số, điều kiện, mâu thuẫn và thiếu claim. Một answer đúng sự
thật nhưng không được context hỗ trợ vẫn là unsupported. Faithfulness cũng không
đo độ đầy đủ hay mức độ trả lời đúng câu hỏi; cần đánh giá riêng answer relevance.

## Quy trình so sánh cấu hình

1. Tạo tập câu hỏi độc lập cho order, inventory, payment và câu hỏi không có nguồn;
   gán relevance trước khi xem câu trả lời. Tách tập chỉnh tham số và tập đánh giá.
2. Lưu corpus version, chunking version, retriever settings, model/prompt version,
   requestId từ LLM measurement, ordered context và answer cho mỗi lần chạy.
3. Reviewer độc lập xem answer + context, tách đủ claims và gán nhãn. Giải quyết
   bất đồng bằng reviewer thứ hai. Giữ annotation và reviewer version để audit.
4. Gửi từng trace tới endpoint; so sánh macro average trên cùng tập câu hỏi,
   báo cả số case null và coverage. Không gộp null thành 0 hay 1.
5. Chọn ngưỡng bằng baseline thực tế, xem các case hồi quy trước khi release.
   Kết hợp latency/token/cost sẵn có để đánh giá đánh đổi chất lượng và chi phí.

Endpoint không lưu dữ liệu hoặc ghi answer/context vào log. Áp dụng cơ chế truy cập
nội bộ tương tự API LLM hiện có. Kiểm thử xUnit bao phủ công thức, empty context,
gold rỗng, thiếu review, contradiction, quote giả, evidence ngoài top k và input sai.
