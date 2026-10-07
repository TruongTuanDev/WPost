# Đặc tả Antigravity kiểm tra và sửa TNVED giới tính cho sản phẩm trên Wildberries và Честный ЗНАК

Phần mềm cần kiểm tra độc lập **mã TNVED có tồn tại**, **mã có phù hợp với cấu tạo thực tế**, **giới tính thương mại có khớp hồ sơ sản phẩm**, và **dữ liệu có đồng bộ với Wildberries cùng Национальный каталог hay chưa**. Chỉ tạo đề xuất sửa khi có căn cứ cho giá trị mới. Dữ liệu thiếu phải tạo yêu cầu bổ sung, không tạo mã đoán.

**Đối tượng sử dụng:** Antigravity và đội phát triển phần mềm quản lý sản phẩm. **Phiên bản đặc tả:** 1.0. **Ngày đối chiếu nguồn:** 07.10.2026. **Phạm vi:** hàng thời trang trong ma trận 104 dòng đã lập, TNVED EAEU, WB và маркировка / Честный ЗНАК. Đây là đặc tả triển khai và dữ liệu tham chiếu có điều kiện, không phải danh mục TNVED đầy đủ hay quyết định phân loại hải quan cho một lô hàng cụ thể.

Tài liệu tự chứa ma trận, quy tắc, hợp đồng dữ liệu, chính sách sửa và ca nghiệm thu. Không cần truy cập cuộc trò chuyện hoặc workbook cũ để hiểu logic. Những tên enum, trạng thái, cổng kiểm tra và chính sách tự động dưới đây là thiết kế của phần mềm; chúng không phải tên trường chính thức của WB hoặc NC.

## 1 Chỉ thị triển khai cho Antigravity

Đọc toàn bộ tài liệu, đặc biệt phần giới tính, sợi pha, cổng kiểm tra và các ca không được tự sửa, trước khi lập trình. Trong dự án hiện có, giữ stack và luồng sản phẩm đang sử dụng; tách lõi kiểm định độc lập với giao diện và connector.

Các yêu cầu bắt buộc:

1. Tạo bộ kiểm định xác định bằng quy tắc có phiên bản. AI chỉ hỗ trợ đọc nhãn, tên hàng, tài liệu và đề xuất thuộc tính; AI không là nơi quyết định cuối cùng cho TNVED.
2. Lưu nguồn gốc từng thuộc tính và bản dữ liệu gốc. Mọi kết quả phải chỉ rõ rule_id, dữ kiện sử dụng, dữ kiện thiếu, nguồn và lý do.
3. Xử lý từng biến thể thực tế theo mẫu hàng, màu, size và quy cách đóng gói. Tách mức kiểm định biến thể khỏi mức trường dữ liệu mà nền tảng cho phép cập nhật.
4. Không suy giới tính thương mại từ TNVED, GTIN, màu sắc, người mẫu trong ảnh hoặc từ khóa trong tiêu đề.
5. Không suy dệt kim/dệt thoi từ độ co giãn, bo chun, chữ cargo, jogger, fleece hoặc джинсы.
6. Không chọn vật liệu bằng tỷ lệ lớn nhất của từng tên sợi hoặc một bảng thứ tự toàn cục. Áp dụng quy tắc Phần XI; trường hợp resolver chưa hỗ trợ phải dừng kết luận vật liệu.
7. Không kết luận mã sai chỉ vì không có trong 104 dòng. Không tự thêm số 0 để biến mã 6/8/9 số thành mã 10 số.
8. Mặc định chạy audit và sinh đề xuất. Tự sửa nội bộ chỉ khi chính sách của chủ phần mềm đã bật và mọi điều kiện trong phần 10 đạt. Quyền cập nhật WB/NC phải được cấu hình riêng theo tài khoản, trường và phạm vi.
9. Không tự tạo GTIN bằng cách ghép TNVED, giới tính, màu hoặc size. Không tái sử dụng GTIN cũ cho một biến thể hàng hóa khác.
10. Không coi HTTP 200, đúng checksum hoặc xuất hiện trong từ điển WB là bằng chứng sản phẩm đã tuân thủ toàn bộ yêu cầu.
11. Không tự tạo subjectID, characteristicID, tên field API mới, enum của NC, token hoặc dữ liệu chứng nhận. Nếu binding API chưa được xác minh, triển khai adapter ở chế độ chỉ đọc hoặc mock rõ ràng.
12. Chỉ công bố chức năng sửa đã qua thử nghiệm, kiểm tra đọc lại và bảo toàn dữ liệu. Không làm một nút “sửa tất cả” áp dụng chung cho mọi loại lỗi.

Kết quả phát triển cần có: importer CSV/XLSX/JSON; hồ sơ thuộc tính có bằng chứng; rule engine; nguồn danh mục TNVED chính thức có phiên bản; adapter WB/NC; báo cáo kiểm định; patch plan; hàng đợi xử lý thủ công; audit log; bộ ca nghiệm thu ở phần 14.

## 2 Tách các lớp đúng sai

| Lớp kiểm tra | Câu hỏi phải trả lời | Điều không được suy ra |
|---|---|---|
| Định dạng TNVED | Có đúng chuỗi 10 chữ số không | 10 số không bảo đảm đó là mã có thật |
| Danh mục TNVED | Có phải mã lá đang có hiệu lực tại ngày áp dụng không | Có thật không bảo đảm đúng loại hàng |
| Phân loại sản phẩm | Tất cả điều kiện kiểu hàng, cấu tạo, vật liệu, nhánh giới tính và chiều cao có khớp không | Khớp ma trận không thay thế bằng chứng về sản phẩm |
| Giới tính thương mại | WB, NC và hồ sơ nhà sản xuất có mô tả cùng đối tượng không | Mã nhánh nữ không đồng nghĩa phải đổi “unisex” thành “nữ” |
| Tương thích WB | subjectID thực tế có chấp nhận mã và giá trị đặc tính này không | Từ điển WB không thay thế TNVED của EEC |
| Nhất quán NC | Đúng GTIN, đúng biến thể, cùng thông tin đã xác minh không | NC có dữ liệu không đồng nghĩa mọi thông tin đã đúng thực tế |
| GTIN | Định dạng, checksum, quan hệ biến thể và kết quả tra cứu có phù hợp không | Checksum đúng không chứng minh GTIN đã cấp hoặc còn hoạt động |
| Nghĩa vụ маркировка | Mặt hàng cụ thể có nằm trong phạm vi và thời điểm áp dụng không | isKiz hoặc một mã TNVED riêng lẻ không đủ cho mọi tình huống |
| Điều kiện lưu thông | Trạng thái mã từng đơn vị, tài liệu phù hợp và nghiệp vụ lưu thông đã đạt chưa | TNVED đúng không tự chứng minh hàng được phép lưu thông |

EEC là nguồn phân loại; WB là nguồn quy tắc nền tảng; GS1/NC là nguồn định danh và đăng ký; hồ sơ sản phẩm là nguồn sự thật về chính sản phẩm. Khi các nguồn mâu thuẫn, lưu mâu thuẫn thay vì chọn nguồn “ưu tiên” để ghi đè toàn bộ. [S01](https://eec.eaeunion.org/comission/department/catr/ett/)[S11](https://dev.wildberries.ru/en/openapi/work-with-products)[S14](https://markirovka.ru/knowledge/tovarnye-gruppy/legkaya-promishlennost/skolko-dolzhno-byt-kartochek-tovarov-dlya-neskolkikh-tsvetov-i-razmerov)[S15](https://www.gs1ru.org/faq/q-hidden-information/)[S20](https://seller.wildberries.ru/instructions/ru/ru/material/verify-product-identifiers?categoryId=labeling-items-and-orders-in-fbs%5C&goBackOption=prevRoute)[S21](https://seller.wildberries.ru/instructions/ru/ru/material/how-to-sell-cim-labeled-items)

Thông báo WB cập nhật 02.10.2026 nêu việc kiểm GTIN từ 01.10.2026 đối với người bán Nga bán trong Nga và hàng phải маркировка. Quy tắc mô tả đầy đủ theo màu và size đã có trong hướng dẫn NC ngày 13.05.2025. Phần mềm phải giữ `seller_country`, `destination_country` và `goods_scope` để áp dụng đúng phạm vi; thiếu bối cảnh thì trả `APPLICABILITY_UNKNOWN`. [S10](https://seller.wildberries.ru/instructions/ru/ru/material/items-and-shipment-labling-like-barcode-and-others)[S14](https://markirovka.ru/knowledge/tovarnye-gruppy/legkaya-promishlennost/skolko-dolzhno-byt-kartochek-tovarov-dlya-neskolkikh-tsvetov-i-razmerov)

## 3 Đơn vị dữ liệu và cách nhập danh sách

Một dòng kiểm định tương ứng **một biến thể hàng hóa thực tế**. Một mẫu có 3 màu và 2 size tạo 6 biến thể. Nhiều chiếc giống hệt nhau của cùng biến thể không tạo thêm GTIN mới chỉ vì khác đơn vị hàng; mã маркировка của từng chiếc là một lớp khác. [S14](https://markirovka.ru/knowledge/tovarnye-gruppy/legkaya-promishlennost/skolko-dolzhno-byt-kartochek-tovarov-dlya-neskolkikh-tsvetov-i-razmerov)[S16](https://markirovka.ru/knowledge/tovarnye-gruppy/legkaya-promishlennost/sostav-koda-markirovki-legprom)

Không dùng riêng tên hàng hoặc vendorCode làm khóa duy nhất. Dùng `tenant_id + product_id + variant_id`; khi đối chiếu WB giữ riêng `nmID` và `chrtID` thực tế. Khi cùng sản phẩm xuất hiện từ nhiều nguồn, giải quyết định danh trước khi so sánh thuộc tính; không ghép bằng độ giống tên.

| Nhóm đầu vào | Trường nội bộ | Yêu cầu |
|---|---|---|
| Định danh | tenant_id, product_id, variant_id, source_record_id | Bắt buộc; mã chuỗi; gắn đúng nguồn |
| Mẫu và tên | brand, manufacturer_model, title_raw, technical_type | Tên thương mại chỉ gợi ý; technical_type phải có căn cứ |
| Nền tảng | seller_country, destination_country, nmID, chrtID, subjectID | Không tự sinh ID; giữ ID dạng chuỗi trong lưu trữ |
| Giới tính | commercial_gender, audience, cut_gender, front_fastening | Tách riêng ý nghĩa, không gộp thành một cột |
| Cấu tạo | construction, product_form, fabric_kind, intended_use | Dệt kim, dệt thoi, loại quần/áo, denim, đồ ngủ… |
| Thành phần | composition_components, determining_component | Tách thân ngoài, lót, nhồi, lớp mặt có lông; tỷ lệ có đơn vị và nguồn |
| Size và màu | color_raw, color_canonical, size_system, size_value, height_min_cm, height_max_cm | Không tự quy đổi size hoặc gộp màu |
| Mã hiện tại | tnved_raw, gtin_raw, barcode_raw | Chuỗi nguyên bản; không ghi đè khi chuẩn hóa |
| NC | nc_card_id, nc_gtin, nc_status, nc_description_type | Không có dữ liệu phải là UNKNOWN, không là “đạt” |
| Chứng cứ | evidence, facts_provenance, source_snapshot | Tài liệu, phiên bản, đường dẫn/locator, ngày và người/hệ thống xác nhận |

Importer phải cho ánh xạ cột trước khi chạy. Các tên `Пол`, `Giới tính`, `Gender` có thể cùng ánh xạ vào giới tính thương mại nếu ngữ cảnh cột đã xác nhận. Không coi chữ `M` trong cột Size là “nam”.

TNVED và GTIN luôn lưu dạng chuỗi. Dữ liệu đã bị Excel chuyển thành số khoa học, mất chữ số đầu hoặc bị làm tròn phải báo `IDENTIFIER_LOSS_SUSPECTED`; không cố tái tạo bằng phỏng đoán. Cho phép bỏ khoảng trắng và dấu phân nhóm chỉ khi toàn bộ chuỗi khớp mẫu phân nhóm được chấp nhận; giữ chuỗi gốc và ghi lại phép chuẩn hóa. Không dùng hàm “xóa mọi ký tự không phải số”.

Tệp CSV xuất cho người dùng cần giữ mã như văn bản theo profile xuất đã kiểm thử. Không thực thi công thức từ trường mô tả. Giá trị trống, `0`, `false` và `unknown` là bốn trường hợp khác nhau.

## 4 Hợp đồng thuộc tính và bằng chứng

Các tên kiểu sau là hợp đồng nội bộ, độc lập ngôn ngữ lập trình. ID nguồn là định danh trong ứng dụng, không phải subjectID hay enum của nền tảng.

```typescript
type EvidenceStatus = "VERIFIED" | "DECLARED" | "INFERRED" | "CONFLICT";
type TriState = true | false | null;
type CommercialGender = "MALE" | "FEMALE" | "UNISEX" | "UNKNOWN";
type TariffGender = "M" | "F" | "F_FALLBACK" | "ANY" | "UNKNOWN";
type Construction = "KNIT" | "WOVEN" | "NONWOVEN" | "MIXED" | "UNKNOWN";
type Audience = "ADULT" | "CHILD" | "INFANT" | "UNKNOWN";

interface Fact<T> {
  value: T | null;
  status: EvidenceStatus;
  evidence_ids: string[];
  extraction_method: "STRUCTURED_SOURCE" | "MANUAL" | "OCR" | "LLM" | "RULE";
  verified_by: string | null;
  verified_at: string | null;
  input_fact_ids: string[];
  resolver_version: string | null;
  derivation_trace: Record<string, unknown> | null;
}

interface Evidence {
  id: string;
  kind: "MANUFACTURER_SPEC" | "LABEL" | "CUSTOMS_DOCUMENT"
      | "CLASSIFICATION_DECISION" | "WB_SNAPSHOT" | "NC_SNAPSHOT"
      | "USER_ATTESTATION" | "PHOTO" | "OTHER";
  locator: string;
  sha256: string | null;
  issued_at: string | null;
  observed_at: string;
  product_scope: string[];
}

interface ProductVariant {
  schema_version: "1.0";
  tenant_id: string;
  product_id: string;
  variant_id: string;
  raw: Record<string, unknown>;
  platform_ids: {
    wb_nmID: string | null; wb_chrtID: string | null;
    wb_subjectID: string | null; nc_card_id: string | null;
  };
  facts: Record<string, Fact<unknown>>;
  current: {
    local: { tnved10: string | null; commercial_gender: string | null };
    wb: Record<string, unknown> | null;
    nc: Record<string, unknown> | null;
  };
  evidence: Evidence[];
}
```

`facts` là từ điển thuộc tính có kiểu trong schema của ứng dụng. Tối thiểu khai báo các tên trong phần 3; không cho một bản ghi tự thêm khóa lạ để vượt điều kiện. `null` biểu thị chưa biết. Nếu hai bằng chứng hợp lệ cho cùng biến thể mâu thuẫn, giá trị dẫn xuất mang `CONFLICT` và giữ cả hai bản khai.

`VERIFIED` cần bằng chứng có phạm vi đúng mẫu, màu/size hoặc lô hàng, và kiểm tra được cách trích xuất. Một dòng mô tả do người bán nhập là `DECLARED`, không tự trở thành `VERIFIED` chỉ vì đã có trên WB. OCR/LLM không tự nâng kết quả của mình lên `VERIFIED`; xác nhận bằng dữ liệu cấu trúc đáng tin cậy hoặc người có trách nhiệm. Điểm tự tin 0,99 của mô hình không thay cho xác minh.

Tách độ chắc chắn của kết quả khỏi kết quả logic: một mã có thể `MATCH` theo dữ liệu khai báo nhưng `evidence_grade=DECLARED_ONLY`. Giao diện phải hiển thị “Khớp theo dữ liệu khai báo”, không hiển thị “Đã xác minh đúng TNVED”. Chỉ dữ kiện đã xác minh mới thỏa điều kiện tự sửa nội dung.

Schema và service phải cưỡng chế: VERIFIED có value khác null đúng kiểu, evidence_ids không rỗng và tồn tại, bằng chứng đúng phạm vi, verified_by là tác nhân được hệ thống tin cậy, verified_at hợp lệ. Không nhận quyền VERIFIED trực tiếp từ cột của file nhập; phải đối chiếu bản xác nhận nội bộ. extraction_method=RULE cần input_fact_ids, resolver_version và derivation_trace; mức xác minh dẫn xuất không được cao hơn các đầu vào quyết định. Kết quả vi phạm trả EVIDENCE_ATTESTATION_INVALID và không mở tự sửa.

Tổng hợp evidence_grade trên **các thuộc tính quyết định kết luận đang xét**: có CONFLICT → CONFLICTED; còn thuộc tính bắt buộc dựa trên INFERRED → INFERRED_ONLY; không có hai trường hợp đó nhưng còn DECLARED → DECLARED_ONLY; tất cả thuộc tính quyết định VERIFIED → VERIFIED. Trường bắt buộc có value=null vẫn tạo NEEDS_DATA, kể cả status bị khai nhầm VERIFIED. Không hạ mức xác minh TNVED chỉ vì một thuộc tính không ảnh hưởng mã, ví dụ mô tả màu, còn thiếu; vẫn báo thiếu ở dimension tương ứng.

Cho phép tái sử dụng hồ sơ mẫu hàng cho nhiều biến thể chỉ khi phạm vi bằng chứng bao phủ các biến thể đó và không có thuộc tính riêng làm đổi phân loại. Không mặc định tất cả màu có cùng thành phần hoặc tất cả size có cùng nhánh chiều cao.

## 5 Quy tắc giới tính

### 5 1 Giới tính thương mại và nhánh thuế quan

`commercial_gender` mô tả đối tượng của hàng trong hồ sơ nhà sản xuất và bản đăng bán. `tariff_gender` là nhánh nam/nữ/không phân biệt giới tính dùng trong quá trình chọn TNVED. Hai biến này không được ghi đè lẫn nhau.

Trong Chương 61 và 62, kiểu cắt may rõ ràng được ưu tiên. Khi kiểu cắt không xác định giới tính, xem quy tắc đóng phía trước trong chú giải 9. Nếu vẫn không thể xác định nam/nữ sau khi đã xem xét đủ, dùng nhánh nữ. **Thiếu dữ liệu để xem xét không phải bằng chứng sản phẩm không phân biệt nam/nữ.** [S02](https://eec.eaeunion.org/upload/files/catr/ett/ru.61_2022_25.04.2022.pdf)[S03](https://eec.eaeunion.org/upload/files/catr/ett/ru.62_2022.pdf)

| Dữ kiện đã xác minh | tariff_gender | Thao tác với commercial_gender |
|---|---|---|
| Kiểu cắt rõ dành cho nam/bé trai | M | So với hồ sơ thương mại riêng |
| Kiểu cắt rõ dành cho nữ/bé gái | F | So với hồ sơ thương mại riêng |
| Kiểu cắt không phân biệt; cách đóng trước xác định theo chú giải là nam | M | Giữ dữ kiện thương mại thực tế |
| Kiểu cắt không phân biệt; cách đóng trước xác định theo chú giải là nữ | F | Giữ dữ kiện thương mại thực tế |
| Đã xác minh thiết kế không thể phân nam/nữ và cách đóng không giải quyết được | F_FALLBACK | Có thể vẫn là UNISEX |
| Chưa biết kiểu cắt/cách đóng; chỉ có nhãn marketing unisex | UNKNOWN | Không tự đổi |
| Mã không chia theo giới tính | ANY trong đánh giá rule | Vẫn kiểm tra giới tính thương mại bằng chứng riêng nếu có |

Giữ giá trị đóng trước dạng đã được đối chiếu với chú giải `LEFT_TO_RIGHT` / `RIGHT_TO_LEFT`. Không tự quyết định từ ảnh gương, vị trí nút trong ảnh hoặc diễn giải trái/phải chưa thống nhất. Nếu không áp dụng được quy tắc đóng phía trước thì ghi lý do.

### 5 2 Những bẫy cần ngăn

Áo T-shirt 6109, một số bộ 6112, khăn, mũ và túi trong ma trận có mã không tách giới tính. Quần áo nam/nữ có thể dùng chung mã ở các nhánh đó; thông tin này không cho phép gộp GTIN của hai sản phẩm thương mại khác nhau. [S02](https://eec.eaeunion.org/upload/files/catr/ett/ru.61_2022_25.04.2022.pdf)[S03](https://eec.eaeunion.org/upload/files/catr/ett/ru.62_2022.pdf)[S08](https://eec.eaeunion.org/upload/files/catr/ett/ru.65_2022.pdf)[S09](https://eec.eaeunion.org/upload/files/catr/ett/ru.42_2022.pdf)[S14](https://markirovka.ru/knowledge/tovarnye-gruppy/legkaya-promishlennost/skolko-dolzhno-byt-kartochek-tovarov-dlya-neskolkikh-tsvetov-i-razmerov)

Ở dòng T075–T076, cột giới tính trong workbook trước mô tả mặt hàng thường gặp. Nhánh 621210 của áo ngực không được chia thành mã nam/mã nữ. Rule pack này đặt `tariff_gender=ANY` cho hai dòng đó; không tạo lỗi giới tính chỉ vì một sản phẩm 6212 có đối tượng thương mại khác thường. Cần xác minh đúng cấu tạo sản phẩm 6212. [S03](https://eec.eaeunion.org/upload/files/catr/ett/ru.62_2022.pdf)

Ví dụ: hoodie cotton dệt kim, đã xác minh kiểu 6110, thiết kế unisex không thể phân nam/nữ, đủ các điều kiện còn lại, có thể đi nhánh `6110209900`. Phần mềm giữ `commercial_gender=UNISEX`; ghi `tariff_gender=F_FALLBACK`. Ngược lại, chỉ thấy tiêu đề “hoodie unisex” thì chưa đủ để chốt nhánh.

Chuẩn hóa giới tính bằng từ điển có ngữ cảnh. “Nam/мужской/men” → MALE; “nữ/женский/women” → FEMALE; “мальчики/девочки” đồng thời cung cấp thông tin đối tượng trẻ em. “Детский” chỉ là trẻ em, chưa xác định giới tính. Việc xuất enum sang WB hoặc NC cần từ điển thực tế của đúng danh mục.

## 6 Quy tắc chất liệu

### 6 1 Từ điển sợi

| Tên thường gặp | Lớp sợi nội bộ | Ghi chú |
|---|---|---|
| Cotton, bông, хлопок | COTTON | Không gộp chung với lanh chỉ vì đều tự nhiên |
| Polyester, полиэстер, полиамид, nylon, acrylic, elastane, spandex | SYNTHETIC | Cộng trong phạm vi resolver hỗ trợ; elastane không được bỏ qua |
| Viscose, вискоза, modal, lyocell, acetate | ARTIFICIAL | Không phải SYNTHETIC |
| Химические волокна | CHEMICAL_UNSPECIFIED | Bao gồm tổng hợp và nhân tạo; cần tách nếu nhánh mã yêu cầu |
| Wool, шерсть | WOOL | Tách cashmere và lông động vật mịn khi mã yêu cầu |
| Cashmere, кашемир | CASHMERE | Không tự dùng nhánh wool thường |
| Silk, шёлк | SILK | Không coi “lụa” marketing là bằng chứng tơ tằm |
| Linen, лён; ramie, рами | FLAX; RAMIE | Có nhánh gộp cụ thể; không phải cotton |
| Bamboo, бамбук, экокожа, microfibre | UNRESOLVED_MARKETING_TERM | Phải xác minh bản chất sợi hoặc lớp mặt |

Định nghĩa synthetic/artificial lấy từ EEC. Từ điển cần phiên bản và bằng chứng cho tên thương mại. [S06](https://eec.eaeunion.org/comission/department/catr/ett/ru.2022/ru.54_2022_08.03.2026.pdf)

### 6 2 Kiểm tra thành phần

Kiểm tra tỷ lệ trên cùng cơ sở khối lượng và cùng bộ phận sản phẩm. Thân ngoài 100% polyester + lớp lót 100% cotton không có nghĩa tổng thành phần là 200% hoặc polyester50/cotton50.

Mỗi bộ thành phần khai là đầy đủ phải cộng đúng 100%. Dùng số thập phân chính xác; sai số kỹ thuật của phép tính máy không phải dung sai nhãn. Nếu nhãn làm tròn khiến tổng khác 100%, ghi `COMPOSITION_TOTAL_REVIEW`; không tự chuẩn hóa tỷ lệ để đủ 100%. Tổng 75+25+5=105% là lỗi dữ liệu, không chọn lại tỷ lệ theo suy đoán.

Chỉ dùng bộ phận quyết định phân loại theo quy tắc áp dụng. Với hàng có nhiều lớp, cấu tạo phức hợp, bề mặt có lông/vòng hoặc phủ tráng, không mặc định lấy toàn bộ khối lượng hay luôn lấy vải nền. Cần `determining_component` đã xác minh và căn cứ. Túi 4202 dùng loại bề mặt ngoài nhìn thấy; không dùng tỷ lệ sợi của lớp lót. [S04](https://eec.eaeunion.org/upload/files/catr/ett/ru.50_2022.pdf)[S05](https://eec.eaeunion.org/upload/files/catr/psn/psn50.pdf)[S09](https://eec.eaeunion.org/upload/files/catr/ett/ru.42_2022.pdf)

### 6 3 Resolver bắt buộc theo cây phân loại

Đầu ra resolver phải chứa `material_resolution`, `method`, `legal_tree_version`, `input_component`, `grouping_steps` và `source_ids`. Nhánh chọn sợi pha có thể phụ thuộc nhóm 50–55, sợi filament/staple và cách gộp các vật liệu; một enum “sợi lớn nhất” không đủ.

Ví dụ trong giải thích EEC: cotton40 + synthetic30 + artificial30 không được kết luận cotton40 chiếm ưu thế; phải xét nhóm hóa học60 trước. Ví dụ linen35 + jute25 + cotton40 cũng không chọn cotton chỉ vì 40 lớn nhất. Đây là ví dụ giải thích vật liệu, không phải đầu vào đủ để tự gán một mã quần áo 10 số. [S05](https://eec.eaeunion.org/upload/files/catr/psn/psn50.pdf)

**Phạm vi tự động tối thiểu của phiên bản đầu:**

| Resolver | Cho phép | Điều kiện |
|---|---|---|
| PURE_CLASS | Một lớp vật liệu nguyên chất trong danh sách dưới đây | Thành phần đầy đủ; bộ phận quyết định đã rõ; không có ngoại lệ bề mặt/phức hợp chưa giải quyết |
| COTTON_SYNTHETIC_ONLY | Chỉ có cotton và các sợi tổng hợp đã xác minh | Gộp tất cả sợi tổng hợp rồi so với cotton; bằng nhau chọn nhánh tổng hợp theo quy tắc phù hợp |
| SPECIALIST_RESOLVED | Kết quả vật liệu do chuyên gia/hồ sơ phân loại xác nhận | Gắn đúng sản phẩm, nhánh áp dụng, nguồn, ngày và người xác nhận; không tự tạo trạng thái này |

PURE_CLASS chỉ nhận: cotton100%; tập sợi tổng hợp đã nhận diện cộng100%; tập sợi nhân tạo đã nhận diện cộng100%; wool100% không gồm lông mịn/cashmere; silk100%; flax100%; ramie100%; cashmere100%; hoặc một loại lông động vật mịn khác đã được định danh riêng100%. Các trường hợp này vẫn cần đúng nhánh của mã và không mở tự động cho family có REVIEW_REQUIRED. OTHER/UNKNOWN/CHEMICAL_UNSPECIFIED không phải lớp nguyên chất.

Hỗn hợp synthetic+artificial không được tự gộp thành PURE_CLASS. Với CHEMICAL_UNSPECIFIED, không tự đổi thành SYNTHETIC. Có thể phát triển phép chứng minh mọi khả năng cùng dẫn tới một nhánh “химические”, nhưng phải là resolver hội tụ riêng đã kiểm thử, không phải lối tắt qua PURE_CLASS. MVP chưa có resolver đó thì giữ NEEDS_DATA ngay cả khi có thể hiển thị ứng viên.

Những hỗn hợp khác trả `MATERIAL_RESOLVER_NOT_IMPLEMENTED` nếu chưa có resolver theo cây EEC đã kiểm thử. Điều này vẫn cho phép tra cứu ma trận và hiển thị ứng viên, nhưng chặn kết luận duy nhất và sửa tự động.

Ví dụ trong phạm vi COTTON_SYNTHETIC_ONLY:

| Thành phần xác minh | Kết quả vật liệu |
|---|---|
| Cotton70 + polyester25 + elastane5 | COTTON |
| Cotton40 + polyester35 + elastane25 | SYNTHETIC, tổng 60 |
| Cotton50 + polyester50 | SYNTHETIC |

Với áo ở nhánh “химические”, material SYNTHETIC hoặc ARTIFICIAL có thể cùng thỏa nhánh. Ngược lại, input chỉ ghi “химические” chưa đủ để chọn giữa hai nhánh synthetic/artificial của quần hoặc váy.

**Ngưỡng len nặng:** giải quyết vật liệu trước, sau đó mới xét nhánh sweater/pullover len ≥600 g và ≥50% len. Áo 600 g với wool50/polyester50 không được tự nhảy sang 6110111000 chỉ vì đủ hai ngưỡng số; hỗn hợp này phải xử lý quy tắc vật liệu trước. Cardigan cũng không tự thành sweater/pullover nặng. [S02](https://eec.eaeunion.org/upload/files/catr/ett/ru.61_2022_25.04.2022.pdf)[S04](https://eec.eaeunion.org/upload/files/catr/ett/ru.50_2022.pdf)

## 7 Chiều cao kiểu hàng và điều kiện loại trừ

### 7 1 Thứ tự xác định phạm vi

Trước khi đối chiếu các dòng thông thường, xác định sản phẩm có thuộc Chương 61/62 hay phải đi nhóm khác; áo ngực 6212 là trường hợp cần nhận diện riêng. Trong phạm vi quần áo và phụ kiện đủ điều kiện của Chương 61/62, xét hàng cho trẻ có chiều cao không quá 86 cm trước, rồi xét ưu tiên 6113/6210 đối với vải đặc biệt/phủ tráng khi áp dụng. Không lấy “độ co giãn” làm tiêu chí chia 61/62. [S02](https://eec.eaeunion.org/upload/files/catr/ett/ru.61_2022_25.04.2022.pdf)[S03](https://eec.eaeunion.org/upload/files/catr/ett/ru.62_2022.pdf)

| Trường hợp | Cách xử lý |
|---|---|
| Người lớn đã xác minh | NON_BABY; không bắt buộc đo chiều cao từng người |
| Trẻ em với dải chiều cao có cận trên ≤86 cm | Xét 6111/6209 nếu loại hàng đủ điều kiện |
| Trẻ em với cận dưới >86 cm | NON_BABY |
| Chỉ có tuổi hoặc chữ “đồ trẻ em” | NEEDS_DATA nếu chiều cao quyết định nhánh |
| Một SKU có dải size vắt qua 86 cm và chưa xác minh cách áp dụng | NEEDS_DATA; không tự lấy cận trên hoặc tách hàng giả |
| Nhiều SKU riêng biệt 80, 86, 92 cùng một mẫu | Phân loại từng SKU, phát hiện xung đột nếu thẻ nền tảng chỉ chứa một TNVED |
| Mũ 65, túi 42 | Không áp dụng ngưỡng 86 để chuyển sang mã quần áo em bé |
| Áo ngực 6212 | Không tự chuyển sang 6209/6111 bằng ngưỡng 86; hồ sơ bất thường cần rà soát |
| Khăn thuộc 6117/6214 dành riêng cho em bé ≤86 cm | Phải xét quy tắc phụ kiện em bé trước; không bỏ qua vì ma trận khăn không ghi giới hạn tuổi |

Các dòng baby trong ma trận chỉ phủ một số nhánh cotton/synthetic và điều kiện cụ thể. Không có nhánh baby artificial trong ma trận không có nghĩa hàng đó không có mã hợp lệ.

Ngưỡng 86 cm là **chiều cao cơ thể trẻ mà sản phẩm được thiết kế phục vụ**, không phải chiều dài quần, vòng ngực, vòng đầu hoặc số size chưa biết hệ đo. Thứ tự ưu tiên baby không được biến thành bộ chặn phủ tráng toàn cục: khi đã xác minh sản phẩm vẫn nằm trong miền Chương 61/62, đủ điều kiện baby và phần vật liệu quyết định đã rõ, ưu tiên 6111/6209 được xét trước 6113/6210. Nếu lớp phủ khiến miền hàng hoặc bộ phận quyết định chưa rõ thì vẫn NEEDS_DATA.

### 7 2 Cổng kiểm tra theo nhóm hàng

`gate_profile` trong ma trận là bộ điều kiện bắt buộc triển khai. Mỗi predicate trả TRUE, FALSE hoặc UNKNOWN và ghi nguồn. Không tạo một checkbox tổng “đã đạt tất cả” từ AI; triển khai từng thuộc tính liệt kê dưới đây. Điều kiện trong `conditions_vi` của từng dòng bổ sung cho gate profile và không được bỏ qua.

| Gate profile | Dữ kiện tối thiểu và điều kiện phân biệt |
|---|---|
| TSHIRT | Dệt kim; hình thái 6109; không cổ mở cài, không dây/bo/siết tại gấu; phạm vi size phù hợp; vật liệu đã giải quyết |
| KNIT_TOP_REGULAR | Kiểu 6110; không áo khoác ngoài; đã loại nhánh nhẹ mỏng cổ kín; nhánh giới tính rõ hoặc fallback có căn cứ |
| KNIT_TOP_FINE_NECK | Kiểu ôm thân trên, cổ polo/cổ cao kín không xẻ; nhẹ mỏng; ≥12 vòng/cm ở cả hai hướng trên mẫu 10×10 cm; vật liệu phù hợp |
| WOOL_TOP_REGULAR | Vật liệu thuộc wool thường, không cashmere/lông mịn khác; kiểu áo; đã loại trường hợp sweater/pullover nặng đồng thời đạt ngưỡng |
| WOOL_PULLOVER_HEAVY | Vật liệu đã chọn nhánh wool; đúng sweater/pullover; khối lượng chiếc ≥600 g và len ≥50%; không suy từ cardigan |
| KNIT_BOTTOM | Quần dài/lửng/short/quần yếm phải rõ; không đồ bơi, quần lót, bộ ski/bộ thể thao; row T013–T022 có điều kiện hình dạng khác nhau |
| WOVEN_TROUSER | Dệt thoi; quần dài/lửng; không short/quần yếm; có/không denim hoặc nhung gân có lông cắt; loại thường, không lao động/bảo hộ |
| WOVEN_SHORT | Dệt thoi; short thường; không đồ bơi, quần lót, quần yếm; giới tính và vật liệu |
| DRESS | Váy liền mặc ngoài; loại trừ váy ngủ/váy lót; cấu tạo và vật liệu; phạm vi nhánh nữ có căn cứ |
| SKIRT | Chân váy/váy quần; không đồ ngủ/nội y; cấu tạo và vật liệu; denim vẫn theo nhánh chân váy |
| KNIT_SHIRT | Không túi dưới eo, không bo/dây siết gấu; ≥10 vòng/cm theo mỗi hướng trên mẫu ≥10×10 cm; tay và cổ xẻ theo loại; không áp điều kiện có tay của nam cho blouse nữ |
| WOVEN_SHIRT | Không túi dưới eo hoặc bo/dây siết gấu; loại nam phải có tay; kiểu áo và cổ xẻ theo định nghĩa; blouse nữ có thể khác |
| SLEEPWEAR | Xác minh thực sự pyjama/áo hoặc váy ngủ thuộc nhánh; không chỉ tên “mặc nhà”; không mọi đồ ngủ liền thân |
| KNIT_UNDERPANTS | Dệt kim; đúng quần lót/quần lót dài; không T-shirt, bra, đồ bơi |
| BRA_SINGLE | Xác minh hình thái áo ngực thuộc 6212; bán riêng, không bộ bra+brief |
| BRA_BRIEF_SET | Bộ bán lẻ thật sự gồm bra và quần lót; không mọi bộ đồ lót |
| WOVEN_OUTER_JACKET | Áo khoác ngoài dệt thoi; không blazer, không hàng thuộc 6210, không bộ ski; bộ phận vật liệu quyết định rõ |
| KNIT_TRACKSUIT | Bộ thể thao thực sự theo giải thích 6112; không gộp T-shirt+short hoặc hoodie+quần chỉ vì bán chung |
| WOVEN_TRACKSUIT_LINED | Bộ thể thao thực sự có lót; mặt ngoài các phần cùng loại vật liệu; không dùng cho bộ không lót |
| BABY_APPAREL | Loại quần áo đủ điều kiện chương tương ứng; chiều cao tối đa ≤86 cm; cotton/synthetic; nhánh knit T085–T086 không gồm găng tay |
| SKI_SUIT | Đúng bộ hoặc đồ liền thân ski theo chú giải; mục đích và cấu tạo rõ; không quần tuyết bán riêng |
| SCARF | Khăn quàng/choàng thường; không cà vạt/mũ; với woven xác minh hình dạng và cạnh để loại trường hợp vuông/gần vuông có tất cả cạnh ≤60 cm |
| BEANIE | Mũ dệt kim bằng sợi dệt; phương pháp làm rõ; không ghép/bện dải, không lưỡi trai, bảo hộ/lông thú; loại các ngoại lệ Chương65 |
| TEXTILE_VISOR_CAP | Mũ có lưỡi trai; cấu tạo đủ6505, không bện/ghép dải thuộc6504; không thuộc nhánh felt lông hoặc felt wool+lông làm từ phôi6501 đứng trước; không bảo hộ/nhựa/cao su/lông thú; loại ngoại lệ Chương65 |
| HANDBAG | Đúng kiểu handbag; không ba lô/du lịch/cặp hồ sơ; xác minh bề mặt ngoài da/nhựa tấm/vải |
| BACKPACK_TRAVEL_SPORT_BAG | Đúng loại ba lô/túi du lịch/thể thao; không tự bao gồm mọi cặp học sinh/hộp/case; mặt ngoài đã xác minh |

Hàng bảo hộ, phủ tráng, áo gile phức hợp, vest/blazer, trang phục truyền thống, hàng đã sử dụng và những sản phẩm ngoài các điều kiện trên phải có rule bổ sung đã kiểm chứng; không rơi vào mã “прочие” mặc định.

Câu trên áp dụng sau thứ tự ưu tiên theo miền; không loại baby chỉ vì có phủ tráng khi các điều kiện Chương 61/62 đã được xác minh. Với mũ, cần kiểm phương pháp sản xuất, loại vật liệu/phôi và các loại trừ hàng đã dùng, đồ chơi/carnival, asbestos của Chương 65. “Có lưỡi trai” hoặc “bằng vải” không đủ để kết luận 6505003000. [S08](https://eec.eaeunion.org/upload/files/catr/ett/ru.65_2022.pdf)

## 8 Thuật toán kiểm định

### 8 1 Ba giá trị logic

`TRUE`: dữ kiện đủ và thỏa. `FALSE`: dữ kiện đủ và không thỏa. `UNKNOWN`: chưa biết hoặc resolver chưa giải quyết. Một điều kiện thiếu không được coi là FALSE, một cổng chưa kiểm tra không được coi là TRUE.

Với AND: có FALSE thì FALSE; không có FALSE nhưng có UNKNOWN thì UNKNOWN; tất cả TRUE thì TRUE. Với OR: có TRUE thì TRUE; không có TRUE nhưng có UNKNOWN thì UNKNOWN; còn lại FALSE.

Ánh xạ Fact sang predicate: value=null, sai kiểu hoặc chưa resolve → UNKNOWN kèm lý do; Fact.CONFLICT → UNKNOWN và cờ xung đột, không được chọn .value của một phía để trả TRUE. Fact.DECLARED hoặc INFERRED có thể dùng cho ứng viên theo giả định nhưng giữ grade chưa xác minh; không mở tự sửa. Fact.VERIFIED chỉ được sử dụng ở mức đó sau kiểm tra bằng chứng trong phần 4. Xung đột có thể ảnh hưởng kết luận phải được ghi nhận trước khi short-circuit AND/OR; kết quả tổng là CONFLICT khi chưa giải quyết.

### 8 2 Luồng xử lý

```text
auditVariant(record, context):
  preserveRawAndResolveIdentity(record)
  normalizeWithoutChangingMeaning(record)
  validateIdentifiersAndComposition(record)
  loadVersionedCatalogs(context.classification_date)
  inspectEvidenceConflicts(record)
  resolveScopeAndProductFamily(record)
  resolveBabyAndSpecialHeadingPriority(record)
  resolveTariffGender(record)
  resolveDeterminingMaterial(record)
  evaluateEveryRelevantRuleWithTriState(record)
  determineClassificationStatus(record)
  checkCommercialGenderAgainstEvidence(record)
  checkWBCompatibilityAndNCConsistency(record)
  validateGTINIdentityWithoutReissuing(record)
  checkCardLevelConsistencyAcrossAllVariants(record)
  createExplainablePatchPlanIfEligible(record)
  returnAuditResultWithSourcesAndVersions()
```

Đánh giá mã dự kiến **độc lập với mã hiện tại**; sau đó mới so sánh. Không dùng mã đang có để điền ngược cấu tạo hoặc giới tính còn thiếu rồi kết luận chính mã đó đúng.

Không chọn “rule đầu tiên khớp”. Gom kết quả tất cả rule có liên quan, sau khi áp dụng đúng các loại trừ và thứ tự ưu tiên pháp lý. Nếu một rule TRUE cho mã A nhưng một rule chưa loại được vì UNKNOWN có thể cho mã B khác, kết quả vẫn chưa đủ để chốt A. Tương tự, hai rule TRUE cho hai mã khác nhau là lỗi/nhập nhằng cần xem xét.

Ma trận có competition_group rộng: APPAREL_TEXTILE, HEADGEAR và BAG_CONTAINER. Chỉ loại một miền bằng bằng chứng về cấu tạo/loại hàng; khi miền chưa rõ thì giữ các miền còn có thể phù hợp. family trong seed là **nhãn của bộ điều kiện kết quả**, không được dùng làm bộ lọc loại rule chỉ vì tên hàng hoặc phân loại AI. Trong APPAREL_TEXTILE, các nhóm top thường, top mỏng cổ kín, wool top, wool nặng, shirt, outerwear và baby phải còn được xem xét khi dữ kiện chưa loại chúng. Không dùng mã hiện tại làm bộ lọc ứng viên. Với104 dòng, ưu tiên đánh giá đầy đủ các rule có thể liên quan thay vì tối ưu sớm làm mất ứng viên.

Một mã duy nhất được đề xuất chỉ khi: có ít nhất một rule TRUE; mọi rule cạnh tranh có thể dẫn đến mã khác đã FALSE bằng dữ kiện; không còn lớp phân loại bắt buộc chưa giải quyết; mã là lá hợp lệ trong danh mục áp dụng; không có xung đột bằng chứng. Ma trận không bao phủ toàn bộ cây pháp lý, nên gate về family/phạm vi phải được xác nhận trước bước này.

### 8 3 Trạng thái đầu ra

| Dimension | Enum bắt buộc |
|---|---|
| identifier_format | VALID, INVALID, MISSING, LOSS_SUSPECTED |
| tariff_catalog | ACTIVE_LEAF, NOT_A_LEAF, ABSENT_CONFIRMED, HISTORICAL, UNKNOWN |
| classification | MATCH, MISMATCH, MISSING_CODE, NEEDS_DATA, NOT_COVERED, AMBIGUOUS, CONFLICT, RULE_ERROR |
| evidence_grade | VERIFIED, DECLARED_ONLY, INFERRED_ONLY, CONFLICTED |
| commercial_gender_check | MATCH, MISMATCH, UNKNOWN, CONFLICT, NOT_APPLICABLE |
| wb_compatibility | ALLOWED, DISALLOWED, UNKNOWN, NOT_CHECKED |
| nc_consistency | MATCH, MISMATCH, UNKNOWN, NOT_CHECKED |
| gtin_local | FORMAT_OK, CHECKSUM_OK, INVALID_FORMAT, BAD_CHECKSUM, UNSUPPORTED_FORMAT, MISSING |
| gtin_registry | CONFIRMED, NOT_FOUND_CONFIRMED, INACTIVE_CONFIRMED, UNKNOWN, NOT_CHECKED |
| marking_scope | REQUIRED, NOT_REQUIRED_CONFIRMED, UNKNOWN, OUTSIDE_CONTEXT |
| applicability theo từng quy tắc hệ thống | APPLICABLE, OUTSIDE_CONTEXT, UNKNOWN |
| correction | NONE, NORMALIZE_ONLY, PROPOSED, ELIGIBLE_LOCAL, BLOCKED, APPLIED_LOCAL, PENDING_EXTERNAL, VERIFIED_EXTERNAL, FAILED_EXTERNAL, OUTCOME_UNKNOWN, PARTIALLY_SYNCED |
| execution_status theo từng đích | NOT_SCHEDULED, PENDING, IN_PROGRESS, SUCCEEDED_VERIFIED, FAILED, OUTCOME_UNKNOWN |
| sync_status của patch plan | NOT_REQUESTED, PENDING, IN_SYNC, PARTIALLY_SYNCED, FAILED, OUTCOME_UNKNOWN |

`MATCH` luôn đi kèm evidence_grade, ngày áp dụng và phạm vi rule. `NOT_COVERED` không bằng `ABSENT_CONFIRMED`. Danh mục chính thức có mã đang hoạt động ngoài 104 dòng thì `tariff_catalog=ACTIVE_LEAF` và `classification=NOT_COVERED` là kết quả hợp lệ.

NOT_A_LEAF dành cho nút cha/prefix thực sự có trong danh mục; ABSENT_CONFIRMED dành cho chuỗi không tồn tại trong snapshot đầy đủ của phạm vi đã kiểm. Chuỗi6103420000 được thêm số0 không phải một nút cha hợp lệ chỉ vì trông giống prefix610342000. Nếu chưa có độ bao phủ đủ để xác nhận vắng mặt thì dùng UNKNOWN.

Không gộp mọi dimension thành một boolean “đạt chuẩn”. Trình bày được trường hợp “TNVED khớp theo hồ sơ; chưa kiểm registry GTIN; WB chưa đồng bộ”. Lỗi ở một lớp không được tự động xóa dữ liệu lớp khác.

`reason_codes` là danh sách chi tiết, tách khỏi enum dimension. Ví dụ GTIN_VARIANT_COLLISION là reason code, không phải giá trị tự thêm vào gtin_local. Lưu `gtin_identity_check=CONSISTENT/COLLISION/UNKNOWN/LEGACY_REVIEW` riêng khi kiểm quan hệ biến thể.

Giữ execution_status riêng cho LOCAL, WB và NC; chỉ tổng hợp sync_status trên các đích có trong patch plan đã được cho phép. VERIFIED_EXTERNAL cần mọi đích từ xa của plan đạt SUCCEEDED_VERIFIED. Một đích thành công, một đích còn chờ → PARTIALLY_SYNCED. Timeout sau gửi mà chưa biết kết quả → OUTCOME_UNKNOWN ở đích đó; phải reconcile trước khi gửi lại.

## 9 Ví dụ kết quả kiểm định và đề xuất

Giả định đã xác minh một biến thể quần dài nam cotton dệt kim, phạm vi NON_BABY, bán riêng, không đồ bơi/nội y/bộ ski/bộ thể thao; mã hiện tại là `6103420000`. Mã 10 số này không phải lá hiện hành trong nhánh đã đối chiếu; quần dài/lửng đủ điều kiện đi `6103420001`, trong khi short thường đi `6103420009`. Không tự sửa nếu chưa biết quần dài hay short. [S02](https://eec.eaeunion.org/upload/files/catr/ett/ru.61_2022_25.04.2022.pdf)

```json
{
  "schema_version": "1.0",
  "variant_id": "DEMO-001-BLACK-M",
  "classification_date": "2026-10-07",
  "rulepack_version": "TNVED-2026-10-07-v1",
  "checks": {
    "identifier_format": "VALID",
    "tariff_catalog": "ABSENT_CONFIRMED",
    "classification": "MISMATCH",
    "evidence_grade": "VERIFIED",
    "commercial_gender_check": "MATCH",
    "wb_compatibility": "NOT_CHECKED",
    "nc_consistency": "NOT_CHECKED",
    "gtin_registry": "NOT_CHECKED"
  },
  "classification": {
    "current_tnved10": "6103420000",
    "candidate_tnved10": ["6103420001"],
    "matched_rule_ids": ["T013"],
    "tariff_gender": "M",
    "commercial_gender": "MALE",
    "material_resolution": "COTTON",
    "missing_fields": [],
    "source_ids": ["S02", "S04"],
    "explanation_vi": "Quần dài dệt kim nam; cotton; đủ các điều kiện của T013."
  },
  "correction": {
    "status": "PROPOSED",
    "target": "LOCAL_DRAFT",
    "changes": [
      {
        "path": "/current/local/tnved10",
        "before": "6103420000",
        "after": "6103420001",
        "reason_code": "TN_CODE_ABSENT_AND_UNIQUE_CLASSIFICATION",
        "rule_ids": ["T013"],
        "evidence_ids": ["DEMO-SPEC-01", "DEMO-LABEL-01"]
      }
    ],
    "external_allowed": false,
    "blockers_external": ["WB_BINDING_NOT_VERIFIED", "NC_NOT_CHECKED"]
  }
}
```

Các định danh DEMO và evidence_ids chỉ phục vụ minh họa. Khi thực thi phải tham chiếu bằng chứng có thật; không copy chúng thành dữ liệu xác minh. `path` là đường dẫn nội bộ, không phải JSON payload của WB.

Patch plan phải có `patch_id`, `tenant_id`, `target_record_id`, `expected_revision_or_hash`, `rulepack_version`, `catalog_snapshot_ids`, `created_at`, `expires_at`, `changes` và `authorization_policy_id` nếu định thực hiện. Token, khóa API và nội dung nhạy cảm không đưa vào tài liệu báo cáo.

## 10 Chính sách sửa dữ liệu

### 10 1 Bốn chế độ vận hành

| Chế độ | Hành vi |
|---|---|
| AUDIT | Chỉ đọc, chuẩn hóa trong bản làm việc và trả lỗi/thiếu dữ liệu |
| PROPOSE | Tạo bản trước/sau và lý do; chưa ghi vào dữ liệu nghiệp vụ |
| APPLY_LOCAL | Sửa các trường nội bộ được chính sách cho phép sau khi đủ cổng kiểm tra |
| SYNC_AUTHORIZED | Áp dụng patch đã đủ điều kiện qua adapter đã xác minh và quyền cấu hình của chủ tài khoản |

Chủ phần mềm có thể cho phép tự áp dụng một nhóm patch rõ ràng. Không yêu cầu duyệt lặp lại cho từng patch nằm hoàn toàn trong chính sách đã được cấp quyền; patch ngoài phạm vi thì giữ trong hàng đợi xử lý. Việc sửa dữ liệu gốc/ghi nền tảng không được suy ra từ quyền chạy audit.

### 10 2 Các loại thay đổi

| Thay đổi | Tự động được đến mức nào |
|---|---|
| Chuẩn hóa khoảng trắng, enum nội bộ theo ánh xạ đã xác nhận | Cho phép trong dữ liệu dẫn xuất; giữ nguyên raw |
| Biểu diễn GTIN13 thành GTIN14 bằng một số 0 đầu | Cho phép tạo trường dẫn xuất; không coi là cấp GTIN mới hoặc tự thay barcode in trên hàng |
| Sửa TNVED nội bộ | Có thể tự áp dụng khi tất cả cổng dưới đây đạt |
| Sửa giới tính thương mại nội bộ | Chỉ sửa giá trị sao chép sai theo bằng chứng thương mại riêng; không suy từ mã hoặc hình người mẫu |
| Sửa thành phần, cấu tạo, giới tính của chính hồ sơ nguồn | Tạo đề xuất và yêu cầu bằng chứng; không “sửa cho khớp mã” |
| Đổi subjectID hoặc cấu trúc thẻ WB | Là thay đổi cấu trúc; đánh giá riêng ảnh hưởng size/thuộc tính/giấy tờ |
| Sửa barcode, tái sử dụng/đổi GTIN | Không thuộc nhóm tự sửa TNVED; xử lý quy trình định danh riêng |
| Sửa thuộc tính bắt buộc của NC đã công bố | Theo quy trình và khả năng thực tế của NC; phần 12 |

### 10 3 Cổng để tự sửa TNVED nội bộ

1. Đã giải quyết đúng định danh biến thể và đúng phạm vi tài khoản.
2. Toàn bộ dữ kiện quyết định đã VERIFIED; không còn dữ kiện suy từ tên/ảnh chưa xác nhận.
3. Có một mã dự kiến duy nhất sau đánh giá đầy đủ điều kiện và ứng viên cạnh tranh.
4. Mã mới được xác nhận là mã lá có hiệu lực trong snapshot áp dụng.
5. Không có hồ sơ hải quan, nhãn, chứng nhận hoặc NC liên quan mâu thuẫn chưa giải quyết. Mâu thuẫn tài liệu không tự chứng minh nguồn nào sai.
6. Rule nằm trong nhóm cho phép tự sửa, phiên bản rule đã qua kiểm thử, nguồn không bị đánh dấu hết hạn/chưa xác minh.
7. Không thay đổi mô tả sự thật về sản phẩm, GTIN, đóng gói, size, màu hoặc giới tính để ép khớp mã.
8. Không có xung đột trong các biến thể cùng trường dữ liệu cấp thẻ.
9. Có chính sách APPLY_LOCAL bật cho trường này; snapshot chưa thay đổi kể từ khi tạo patch.
10. Ghi được before/after, lý do, bằng chứng và khả năng khôi phục bản nội bộ.

Ở phiên bản đầu, những family phức tạp trong ma trận có `local_semantic_auto_policy=REVIEW_REQUIRED`. Đây là lựa chọn triển khai của sản phẩm, không phải EEC cấm tự động hóa. Có thể mở quyền sau khi bổ sung predicate, bằng chứng và kiểm thử chuyên biệt.

Nhóm lỗi `NEEDS_DATA`, `NOT_COVERED`, `AMBIGUOUS`, `CONFLICT`, `RULE_ERROR` và `MATERIAL_RESOLVER_NOT_IMPLEMENTED` không có nút tự sửa nội dung. Giao diện cho phép bổ sung đúng trường thiếu hoặc gửi vào quy trình phân loại chuyên môn.

## 11 Đồng bộ với Wildberries

### 11 1 Adapter và từ điển thực tế

Từ điển TNVED theo danh mục và đặc tính sản phẩm là dữ liệu động. Có thể dùng các endpoint đã được tài liệu WB mô tả, nhưng phải đối chiếu phiên bản hiện hành khi viết connector: [S11](https://dev.wildberries.ru/en/openapi/work-with-products)

| Nhu cầu | Điểm tích hợp tham chiếu | Cách dùng |
|---|---|---|
| Đọc đặc tính danh mục | GET /content/v2/object/charcs/{subjectId} | Lấy ID, kiểu và ràng buộc của thuộc tính đúng subject |
| Giá trị giới tính WB | GET /content/v2/directory/kinds | Lấy từ điển giới tính của nền tảng; đối chiếu thêm ràng buộc của subject, không suy giá trị từ mã TNVED |
| TNVED theo danh mục | GET /content/v2/directory/tnved với subjectID | Kiểm mã có trong tập cho phép; giữ isKiz riêng |
| Đọc thẻ | POST /content/v2/get/cards/list | Lấy đầy đủ thẻ và các biến thể liên quan, phân trang hoàn chỉnh |
| Cập nhật thẻ | POST /content/v2/cards/update | Chỉ dùng adapter đã xác minh schema và ngữ nghĩa cập nhật |

Không dùng subjectID mẫu trong tài liệu làm ID thật. Tên danh mục giống nhau không đủ để xác định cùng subjectID. Không dùng phép so substring “nam”/“nữ” trên tên mã.

Hướng dẫn giao diện WB hiện có cảnh báo chuyển thuộc tính cũ `Код ТН ВЭД` sang field `ТН ВЭД` riêng. Vì vậy không hardcode characteristicID cũ; lưu `api_schema_version` và mapping đã kiểm chứng của tài khoản/locale đang dùng. Tài liệu này không xác nhận nesting hiện hành của field TNVED mới hoặc supplementary gtin trong request JSON. Nếu chưa xác nhận được, trả `CONNECTOR_BINDING_UNVERIFIED` và chỉ sinh đề xuất. [S13](https://seller.wildberries.ru/instructions/ru/by/material/how-to-create-card)[S22](https://t.me/wb_api_notifications/479)

### 11 2 Kiểm tra xung đột theo cấp thẻ

Audit từng SKU/size trước, sau đó nhóm theo đối tượng mà field TNVED/giới tính áp dụng. Không giả định WB cho TNVED riêng từng size. Adapter phải công bố `field_scope` bằng schema thực tế.

Nếu một thẻ cần hai mã khác nhau, ví dụ size80 → baby và size92 → quần áo trẻ lớn, trả `CARD_TNVED_CONFLICT`. Không lấy mã của size đầu tiên cho cả thẻ. Đề xuất kế hoạch cấu trúc thẻ phù hợp sau khi kiểm tra quy tắc WB; không tự tách/gộp, chuyển hàng tồn hoặc thay GTIN.

Các sản phẩm cùng mã TNVED nhưng khác giới tính thương mại vẫn cần dữ liệu đúng từng sản phẩm. Một nhóm thẻ gộp để hiển thị không phải một biến thể định danh duy nhất.

### 11 3 Ghi dữ liệu và kiểm tra kết quả

Tài liệu WB nêu cập nhật thẻ có ngữ nghĩa ghi lại thẻ, không nên coi như PATCH nhỏ. Barcode có các giới hạn riêng, gồm khả năng thêm nhưng không tùy ý sửa/xóa qua luồng đó. HTTP 200 chưa đủ để kết luận từng thẻ đã thay đổi thành công. [S11](https://dev.wildberries.ru/en/openapi/work-with-products)[S12](https://dev.wildberries.ru/en/news/101)

Luồng connector cần:

1. Gộp patch tương thích và khóa theo tenant + seller + nmID trong ứng dụng; đọc snapshot hiện hành đủ các trường có thể ghi và mọi biến thể liên quan.
2. Ngay trước khi gửi, so revision/hash với snapshot của patch và kiểm expires_at, phiên bản/hash rule, EEC catalog, từ điển WB, schema adapter cùng quyền hiện hành. Nếu có thay đổi, hết hạn hoặc refresh thất bại thì vô hiệu hóa patch và đánh giá lại; không ghi đè mù.
3. Lập payload theo danh sách trường ghi được của schema đã xác minh. Không chỉ gửi hai trường TNVED+gender; cũng không echo nguyên response GET có trường chỉ đọc.
4. Giữ nguyên các giá trị không thuộc patch, đặc biệt size, barcode, đặc tính, mô tả và các liên kết chứng từ nếu schema yêu cầu bảo toàn.
5. Gửi theo batch/rate limit hiện hành; ghi kết quả riêng từng thẻ.
6. Kiểm tra lỗi nghiệp vụ và đọc lại sau đồng bộ. Chỉ chuyển `VERIFIED_EXTERNAL` khi các field đích đúng và các field cần bảo toàn không mất.
7. Retry có giới hạn với backoff cho lỗi tạm thời; không retry tự động lỗi dữ liệu, quyền hoặc schema.
8. Nếu timeout sau gửi, trạng thái là `OUTCOME_UNKNOWN`; đọc lại trước khi gửi lại.

Khóa trong ứng dụng và kiểm hash ở client không phải thao tác compare-and-swap trên server. Tác nhân ngoài ứng dụng vẫn có thể cập nhật sau lần đọc cuối. Chưa xác minh khả năng ETag/If-Match/CAS phía WB; không tự thêm các trường/header như thể đã được API hỗ trợ. Giảm khoảng thời gian đọc–ghi, kiểm lại sau ghi và đưa mâu thuẫn vào hàng đợi xử lý.

Thiết kế không được giả định có giao dịch nguyên tử chung WB–NC; khả năng đó chưa được xác minh trong đặc tả. Nếu WB đã đổi nhưng NC còn chờ, trạng thái `PARTIALLY_SYNCED`; không báo “đã đồng bộ”. Khôi phục nội bộ khác với đảo ngược tác vụ từ xa; không cam kết rollback từ xa khi nền tảng không hỗ trợ.

## 12 NC GTIN và giới hạn sửa thuộc tính

### 12 1 Những kiểm tra không được gộp chung

GTIN không chứa giới tính, chất liệu hay TNVED trong cấu trúc chữ số. Phần mềm tra thuộc tính qua dữ liệu đăng ký hoặc hồ sơ liên quan, không “giải mã” thuộc tính từ GTIN. [S15](https://www.gs1ru.org/faq/q-hidden-information/)

Profile GTIN của đặc tả này nhận GTIN13/14. GTIN13 được biểu diễn trong trường 14 ký tự bằng cách thêm một số 0 đầu, giữ nguyên định danh. Các độ dài GS1 khác có thể hợp lệ trong ngữ cảnh khác nhưng ở profile này trả `UNSUPPORTED_FORMAT`, không kết luận vô hiệu trên toàn cầu. Không xóa chữ số, cắt đuôi hoặc thêm 0 cho mã dài bất kỳ. [S26](https://support.gs1.org/support/solutions/articles/43000734355-what-is-the-required-format-of-gtin-in-gs1-edi-standards-)

Thuật toán checksum: bỏ chữ số kiểm tra cuối; duyệt từ phải sang trái phần dữ liệu với trọng số 3,1 xen kẽ, bắt đầu 3; check digit = (10 − tổng mod 10) mod 10. Đúng checksum chỉ là kiểm tra toán học. [S27](https://www.gs1.org/services/how-calculate-check-digit-manually)

Nếu input là toàn bộ Data Matrix, dùng parser GS1 đã kiểm chứng để lấy AI(01); không lấy 14 ký tự đầu hoặc dùng cả Data Matrix làm GTIN. GTIN và serial của từng chiếc là hai trường khác nhau. [S16](https://markirovka.ru/knowledge/tovarnye-gruppy/legkaya-promishlennost/sostav-koda-markirovki-legprom)

Đối chiếu GTIN trùng sau chuẩn hóa 13/14 theo **định danh biến thể**, không theo số dòng. Nhiều dòng kho/lô hàng của cùng một biến thể có thể cùng GTIN; hai màu hoặc hai size hàng hóa khác nhau trong mô tả đầy đủ cần xử lý xung đột. Khóa biến thể còn có mẫu, nhãn hiệu, quy cách/loại đóng gói và các khác biệt tạo hàng hóa riêng; “màu+size” không là khóa toàn cục. [S14](https://markirovka.ru/knowledge/tovarnye-gruppy/legkaya-promishlennost/skolko-dolzhno-byt-kartochek-tovarov-dlya-neskolkikh-tsvetov-i-razmerov)

Giữ riêng trạng thái thẻ NC, trạng thái GTIN/đăng ký và trạng thái mã từng đơn vị đang lưu thông. Một trường Boolean “GTIN active” không thay cho ba loại kiểm tra này. API không truy cập được → UNKNOWN, không PASS hoặc NOT_FOUND.

Kết quả registry CONFIRMED phải ghi provider, ngày truy vấn và chính xác những gì đã xác minh: tồn tại, dữ liệu trả về, trạng thái và quan hệ với biến thể. Không coi đó là xác nhận tất cả yêu cầu lưu thông. GTIN có sẵn của nhà sản xuất có thể được sử dụng trong chuỗi bán lại; không bắt buộc GTIN phải do chính shop cấp mới chỉ để qua bộ kiểm tra WB. [S10](https://seller.wildberries.ru/instructions/ru/ru/material/items-and-shipment-labling-like-barcode-and-others)

Lưu mỗi RegistryObservation với provider, query_scope, queried_gtin14, checked_at, snapshot_id, raw_status, normalized_status và failure_reason. query_scope phân biệt ALLOCATION, GTIN_TO_ITEM, NC_PUBLICATION và các phép kiểm được provider thực sự hỗ trợ. nc_card_state giữ raw_status và normalized_status theo adapter có phiên bản; không dùng trạng thái ACTIVE của GTIN thay cho Published của thẻ. Timeout là UNKNOWN kèm UNAVAILABLE. Một provider chỉ trả dữ liệu chủ mã không tự xác nhận sản phẩm hoặc quyền sửa thẻ.

### 12 2 Màu size và hàng tồn mô tả rút gọn

Hệ size và giá trị size là cặp dữ liệu. Không tự chuyển S/M/L sang 42/44/46. Một SKU thực sự có khoảng size 46–48 khác với hai SKU riêng size46 và size48. ONESIZE chỉ khi hàng thật sự là onesize. Màu `РАЗНОЦВЕТНЫЙ` chỉ dùng khi sản phẩm thật sự đa màu, không để gộp các màu đơn sắc. [S17](https://markirovka.ru/knowledge/tovarnye-gruppy/legkaya-promishlennost/kak-korrektno-ukazat-razmer-odezhdy-v-kartochke-tovara-legprom)[S25](https://markirovka.ru/community/shoes-and-clothes/samye-populyarnye-voprosy-po-opisaniyu-kartochek-tovarov-pri-markirovke-odezhdy)

Trường hợp tồn kho đã có mô tả rút gọn hoặc supplementary GTIN phải có căn cứ riêng, đúng thời điểm và trạng thái thực tế. Không đánh dấu ngoại lệ chỉ vì phát hiện mã trùng; không dùng quy trình tồn kho để tạo hàng mới chung GTIN. Profile mặc định `FULL_DESCRIPTION`; `LEGACY_SHORT_DESCRIPTION` cần bằng chứng và đánh giá riêng. [S23](https://t.me/wbsellerofficial/6808)

### 12 3 Sửa dữ liệu NC

Hướng dẫn NC ngày 03.08.2026 mô tả yêu cầu sửa thuộc tính bắt buộc qua bộ phận hỗ trợ với lý do, phương án lưu thông và biểu mẫu theo nhóm; có biểu mẫu cho quần áo/đồ lót. Hướng dẫn thí điểm tự sửa ngày 19.08.2026 giới hạn ở các nhóm được nêu, không gồm quần áo, và các trường TNVED/thành phần nằm trong danh sách không cho sửa của chức năng đó. Vì vậy không xây một lời hứa “auto sửa trực tiếp TNVED, giới tính, thành phần mọi thẻ NC đã công bố”. [S18](https://markirovka.ru/knowledge/tovarnye-gruppy/obschie-voprosy-gis/vnesenie-izmeneniy-v-atributy-kartochek-tovarov-v-kmt)[S19](https://markirovka.ru/knowledge/tovarnyegruppy/obschie-voprosy-gis/kak-sformirovat-shablon-zayavki-na-vnesenie-izmeneniy-v-obyazatelnye-atributy-v-kartochke)

Cho phép phần mềm lập `NC_CHANGE_REQUEST` gồm: GTIN/thẻ xác minh; thuộc tính trước/sau; bằng chứng; lý do; trạng thái thẻ; phương án lưu thông; các field liên quan cần đồng thời sửa, ví dụ tên đầy đủ có chứa giá trị sai. Lấy mẫu hiện hành từ NC theo đúng nhóm, không tự dựng mẫu Excel và gọi đó là mẫu chính thức.

Kiểm điều kiện thẻ theo quy trình hỗ trợ: nguồn hướng dẫn yêu cầu thẻ ở trạng thái Опубликована và xử lý phiên bản nháp/chờ duyệt/chờ ký liên quan trước. Phần mềm phải phát hiện trạng thái này và chỉ dẫn quy trình phù hợp; không tự công bố hoặc ký bản nháp chỉ vì muốn sửa TNVED. [S18](https://markirovka.ru/knowledge/tovarnye-gruppy/obschie-voprosy-gis/vnesenie-izmeneniy-v-atributy-kartochek-tovarov-v-kmt)

Chuẩn bị gói yêu cầu là thao tác nội bộ. Gửi email/ЭДО hoặc yêu cầu ra ngoài là hành động khác, chỉ thực hiện khi có quyền gửi được cấp rõ ràng. Kết quả vẫn PENDING_EXTERNAL cho đến khi có xác nhận sửa và đọc lại.

Sửa lỗi khai báo của **cùng một hàng hóa** không tự động có nghĩa phải phát hành GTIN mới. Ngược lại, không dùng lý do “sửa lỗi” để đổi GTIN hiện hữu sang một màu/size hoặc loại hàng hóa khác. Xác định thay đổi bản chất hàng hóa theo quy trình định danh hiện hành trước khi yêu cầu cấp mới. [S14](https://markirovka.ru/knowledge/tovarnye-gruppy/legkaya-promishlennost/skolko-dolzhno-byt-kartochek-tovarov-dlya-neskolkikh-tsvetov-i-razmerov)[S18](https://markirovka.ru/knowledge/tovarnye-gruppy/obschie-voprosy-gis/vnesenie-izmeneniy-v-atributy-kartochek-tovarov-v-kmt)

Các kiểm tra giấy tờ phù hợp từ các mốc 2026 nên là module riêng; nếu chưa triển khai thì hiển thị NOT_CHECKED, không phát hành kết luận “đủ điều kiện lưu thông” chỉ nhờ TNVED đúng. [S24](https://markirovka.ru/knowledge/tovarnye-gruppy/legkaya-promishlennost/proverka-razreshitelnykh-dokumentov-pri-vvode-v-oborot-s-1-sentyabrya-2026-goda)

## 13 Phiên bản dữ liệu và báo cáo vận hành

Mỗi lần audit phải lưu `classification_date`, `audit_executed_at`, `rulepack_version`, `tariff_catalog_version`, `wb_dictionary_snapshot_id`, `nc_snapshot_id` và hash đầu vào. Ngày đối chiếu 07.10.2026 không phải ngày bắt đầu hiệu lực của mọi mã.

Rule pack 104 dòng là tập ứng dụng có điều kiện. Danh mục mã lá đầy đủ phải có cơ chế nhập từ nguồn EEC, hiệu lực từ/đến, trạng thái mã cha/mã lá và quan hệ thay thế nếu có nguồn chính thức. Không dùng regex 10 số để xây danh mục hợp lệ; không lấy tên file có “2022” làm lý do khẳng định lỗi thời nếu nó vẫn được EEC liên kết trong danh mục hiện hành.

Từ điển WB cần refresh theo tài khoản/subject và có TTL cấu hình. Chính sách ban đầu có thể yêu cầu rà soát rule pháp lý sau tối đa 30 ngày; đây là lựa chọn vận hành, không phải thời hạn pháp luật. Thay đổi nguồn hoặc schema phải vô hiệu hóa cache liên quan và chạy lại tập hồi quy trước khi cho tự sửa.

Mỗi dòng báo cáo nên có:

```text
product_id | variant_id | nmID | chrtID | subjectID
title | color | size_system | size_value
tnved_before | tnved_candidates | tnved_after_proposed
commercial_gender_before | commercial_gender_expected | tariff_gender
construction | material_resolution | determining_component
identifier_format | tariff_catalog | classification | evidence_grade
gender_check | wb_compatibility | nc_consistency | gtin_status
missing_fields | reason_codes | rule_ids | evidence_ids | source_ids
correction_status | blockers | policy_id | snapshot_version | checked_at
```

Giao diện ưu tiên “Vấn đề gì”, “Cần bổ sung gì”, “Giá trị đề xuất và vì sao”, “Được áp dụng đến đâu”. Cho lọc theo lỗi, thiếu thông tin, không có rule, mâu thuẫn, đề xuất và kết quả đồng bộ. Không hiển thị thông báo mơ hồ “AI thấy sai”.

Audit log append-only phải đủ để tái lập quyết định. Chạy lại cùng dữ liệu, cùng snapshot và cùng policy phải cho cùng kết quả. Tách log lý do khỏi dữ liệu bí mật của connector.

## 14 Ca nghiệm thu bắt buộc

Các ca dưới đây là **kết quả mong đợi cho phần mềm sẽ xây dựng**, không phải thông báo rằng phần mềm của bạn đã chạy qua các bài test.

Quy ước cho ca có kết luận mã cụ thể: tất cả gate không được nêu là vấn đề đã được điền tường minh và xác minh; hàng mới, phạm vi thông thường, không có ngoại lệ phủ tráng/bảo hộ/bộ hàng chưa giải quyết; resolver và snapshot phù hợp. Khi viết test thực tế phải tạo fixture đầy đủ, không để giá trị thiếu rồi ngầm mặc định “đã đạt”.

Ca chỉ kiểm một dimension phải assert riêng dimension đó và không ép các dimension chưa kiểm thành PASS. Ca có mã đúng chưa đồng nghĩa được tự áp dụng: vẫn áp policy của family và chế độ vận hành.

| ID | Đầu vào hoặc tình huống | Kết quả bắt buộc | Hành vi phải ngăn |
|---|---|---|---|
| C001 | Quần dài nam dệt kim cotton; mã hiện tại 6103420000 | MISMATCH + ABSENT_CONFIRMED; đề xuất 6103420001 theo T013 khi đủ snapshot | Không tự đổi nếu thiếu hình dạng quần |
| C002 | Short nam dệt kim cotton; hiện tại dùng mã quần dài | Đề xuất 6103420009 theo T016 | Không dùng 6103420001 |
| C003 | Short nam dệt kim cotton 40/polyester 35/elastane 25 | SYNTHETIC60; mã 6103430009 theo T017 | Không chọn cotton 40 vì lớn nhất từng dòng |
| C004 | Quần nữ dệt kim cotton với mã nam hiện tại | Đề xuất 6104620000 theo T019 khi giới tính/cấu tạo có bằng chứng | Không đổi giới tính thương mại thành nam để khớp mã cũ |
| C005 | Hoodie cotton dệt kim unisex; thiết kế và cách đóng đã được đánh giá đủ, không phân biệt | T005; 6110209900; tariff_gender=F_FALLBACK; commercial_gender=UNISEX | Không sửa unisex thành nữ |
| C006 | Chỉ tiêu đề hoodie unisex; thiếu kiểu cắt/cách đóng | NEEDS_DATA; tariff_gender=UNKNOWN | Không tự áp fallback vì dữ liệu thiếu |
| C007 | Hai T-shirt cotton: một sản phẩm nam, một sản phẩm nữ, đủ kiểu 6109 | Cả hai có thể 6109100000; kiểm thương phẩm và GTIN riêng | Không gộp hai sản phẩm thành một GTIN |
| C008 | Jeans nam cotton 98/elastane 2; denim dệt thoi xác minh; loại thường | 6203423100 theo T023 | Co giãn không biến thành dệt kim |
| C009 | Jeans nữ cotton 98/elastane 2; denim dệt thoi xác minh; loại thường | 6204623100 theo T028 | Không dùng mã jeans nam |
| C010 | Tên jeans/jogger; chưa biết cấu tạo vải | NEEDS_DATA construction và fabric_kind | Không suy chương 61 hoặc 62 từ tên |
| C011 | Váy liền dệt kim nữ cotton 50/polyester 50 | 6104430000 theo T040 sau resolver; SYNTHETIC | Không dùng 6104420000 |
| C012 | Váy liền dệt thoi nữ 100% viscose | 6204440000 theo T044 | Không coi viscose là synthetic để chọn 6204430000 |
| C013 | Thành phần 75 cotton+25 polyester+5 elastane | NEEDS_DATA hoặc CONFLICT; INVALID_COMPOSITION_TOTAL; chặn sửa | Không rescale hoặc tự giảm một tỷ lệ |
| C014 | Thân ngoài 100 polyester; lót 100 cotton; chưa rõ bộ phận quyết định | NEEDS_DATA COMPONENT_CLASSIFICATION_UNRESOLVED | Không cộng các lớp thành hỗn hợp 50/50 |
| C015 | Cotton 40/polyester 30/viscose 30; chưa có resolver mở rộng | NEEDS_DATA MATERIAL_RESOLVER_NOT_IMPLEMENTED | Không chọn cotton; không lấy expert_reference_code làm fallback |
| C016 | Linen 35/jute 25/cotton 40; chưa có resolver mở rộng | NEEDS_DATA MATERIAL_RESOLVER_NOT_IMPLEMENTED | Không dùng MAX flat để chọn cotton |
| C017 | Áo nỉ cotton 70/polyester 25/elastane 5; kiểu 6110 nam xác minh | 6110209100 theo T004; cotton chiếm ưu thế trong profile hỗ trợ | Không bỏ 5% elastane khỏi dữ liệu |
| C018 | Quần áo baby cotton dệt kim thiết kế chiều cao 80 cm | 6111209000 theo T085 nếu đúng loại và loại trừ | Không dùng mã trẻ lớn |
| C019 | Cùng trường hợp, chiều cao tối đa 86 cm | 6111209000 theo T085 | Ngưỡng bao gồm 86 |
| C020 | Quần dài bé trai cotton dệt kim chiều cao 92 cm | 6103420001 theo T013 nếu đủ điều kiện | Không kéo mã 6111209000 sang 92 |
| C021 | Một thẻ có SKU 80 cm và SKU 92 cm, field TNVED ở cấp thẻ | CARD_TNVED_CONFLICT; chặn đồng bộ mã chung | Không last-write-wins giữa hai size |
| C022 | Cột size ghi 80 nhưng thực tế là vòng ngực hoặc chưa rõ đơn vị | NEEDS_DATA size_measure_type | Không coi số 80 là chiều cao trẻ |
| C023 | Một SKU thật sự ghi dải chiều cao 86–92 cm | NEEDS_DATA về phạm vi áp dụng; không tự tách biến thể | Không tự chọn baby hoặc lấy cận trên |
| C024 | Jacket nữ dệt thoi, nhánh hóa học xác minh, nặng 1,2 kg | 6202400009 theo T080; review policy của family | Không chuyển 0001 chỉ do nặng hơn 1 kg |
| C025 | Coat nữ nhánh hóa học >1 kg, mã 6202400001 và đủ hồ sơ | ACTIVE_LEAF nhưng NOT_COVERED trong 104 dòng | Không sửa sang 6202400009 để khớp bảng |
| C026 | Polo nam cotton dệt kim có xẻ/cài, đúng 6105 và mật độ≥10 vòng/cm | 6105100000 theo T051; review policy | Không tự chọn 6110201000 chỉ vì chữ polo |
| C027 | Áo sơ mi dệt kim 9,9 vòng/cm mỗi hướng; thiếu căn cứ nhánh khác | Không thỏa 6105/6106; cần phân loại bổ sung | Không làm tròn 9,9 thành 10 |
| C028 | Áo nhẹ mỏng cổ kín cotton đúng hình thái và 12 vòng/cm cả hai hướng | 6110201000 theo T008; review policy | Phải thỏa tất cả điều kiện, không chỉ mật độ |
| C029 | Pullover 100% wool nặng 600 g; đúng hình thái, đủ điều kiện | 6110111000 theo T012; review policy | Bao gồm mốc 600 g |
| C030 | Sweater 600 g wool 50/polyester 50, resolver MVP chưa hỗ trợ | NEEDS_DATA MATERIAL_RESOLVER_NOT_IMPLEMENTED | Không nhảy 6110111000 vì 50% wool và 600 g |
| C031 | Bra bán riêng đúng cấu tạo 6212 | 6212109000 theo T075; tariff_gender=ANY | Không ép giới tính thương mại từ cột nữ trong workbook |
| C032 | Bộ bán lẻ đúng bra+brief | 6212101000 theo T076 | Không dùng cho mọi bộ đồ lót |
| C033 | Hoodie+jogger đóng cùng túi nhưng chưa chứng minh bộ thể thao pháp lý | NEEDS_DATA REVIEW_SET_CLASSIFICATION | Không mặc định 6112110000/6112120000 |
| C034 | Quần tuyết bán riêng; chưa rõ cấu tạo chi tiết | Không đủ điều kiện SKI_SUIT; cần phân loại quần | Không chọn 6112200000/6211200000 chỉ vì chữ ski |
| C035 | Khăn silk dệt thoi vuông 60 × 60 cm cho người lớn | Không áp T092; xem 6213 ngoài ma trận | Không chọn 6214100000 |
| C036 | Mũ beanie dệt kim acrylic; hàng thường, không lưỡi trai/bảo hộ/lông thú | 6505009000 theo T097 | Tên tiếng Việt mũ len không chứng minh wool |
| C037 | Mũ lưỡi trai vật liệu dệt đủ 6505 | 6505003000 theo T098 | Không dùng mã beanie |
| C038 | Túi tên экокожа, chưa xác minh mặt ngoài | NEEDS_DATA outer_visible_surface | Không mặc định da thật hoặc plastic sheet |
| C039 | Handbag có mặt vải xác minh, đủ điều kiện 4202 | 4202229000 theo T101; review policy; marking_scope=UNKNOWN nếu chưa tra | Không khẳng định tất cả 4202 phải маркировка |
| C040 | Khăn dành riêng baby chiều cao≤86 cm | Xét baby routing trước; không tự áp 6117100000/6214 | Không bỏ qua nhóm phụ kiện em bé |
| C041 | Mã 6202400001 có trong danh mục đầy đủ nhưng ngoài ma trận | tariff_catalog=ACTIVE_LEAF; classification=NOT_COVERED | Không đánh mã không tồn tại |
| C042 | Input 610342 hoặc 610342000 | identifier_format=INVALID hoặc profile yêu cầu bổ sung; không có auto-fix | Không pad 0 thành 10 chữ số |
| C043 | Input TNVED trong dạng 6.10342E+09 đã mất độ chính xác/nguồn gốc | LOSS_SUSPECTED; yêu cầu dữ liệu nguồn nguyên vẹn | Không reconstruct mã bằng đoán |
| C044 | Vật liệu không biết, có dòng OTHER trong ma trận | NEEDS_DATA | UNKNOWN không bằng OTHER |
| C045 | Một rule TRUE choA; một rule UNKNOWN có thể choB khác | NEEDS_DATA/AMBIGUOUS; candidates gồm khả năng chưa loại | Không chọnA chỉ vì TRUE đầu tiên |
| C046 | Hai rule TRUE ra hai mã khác nhau | AMBIGUOUS hoặc RULE_ERROR; chặn sửa | Không chọn theo thứ tự bảng |
| C047 | Đầy đủ dữ liệu khai báo nhưng chưa có bằng chứng xác minh | Có thể MATCH + DECLARED_ONLY; không ELIGIBLE_LOCAL | Không hiển thị đã xác minh đúng |
| C048 | Nhãn cùng biến thể ghi nam, NC ghi nữ; chưa giải quyết mâu thuẫn | CONFLICT theo phạm vi bằng chứng; lập đề xuất tìm nguồn đúng | Không chọn nguồn mới hơn chỉ theo thời gian crawl |
| C049 | Mã phù hợp EEC nhưng không nằm trong từ điển subject ID của WB | classification=MATCH; wb_compatibility=DISALLOWED | Không chọn mã sai thực tế chỉ để WB chấp nhận |
| C050 | subject ID hoặc binding field TNVED mới chưa xác minh | wb_compatibility=UNKNOWN; CONNECTOR_BINDING_UNVERIFIED; chặn remote | Không tự invent ID/nesting |
| C051 | GTIN-13 4006381333931 trong test toán học | Dẫn xuất 04006381333931; checksum đúng; registry=NOT_CHECKED | Đây là fixture, không cấp GTIN cho hàng thật |
| C052 | GTIN4006381333932 trong test toán học | BAD_CHECKSUM | Không tự sửa chữ số cuối của GTIN được nhập |
| C053 | GTIN có độ dài 12 trong profile 13/14 | UNSUPPORTED_FORMAT | Không tuyên bố mọi GTIN-12 đều vô hiệu |
| C054 | Hai dòng kho cùng một biến thể, cùng GTIN | Không báo trùng biến thể chỉ vì trùng dòng | Không yêu cầu GTIN mới cho từng chiếc |
| C055 | Hai màu/size khác nhau dùng cùng GTIN dưới FULL_DESCRIPTION | GTIN_VARIANT_COLLISION; chặn tái gán | Không tự tạo GTIN theo mẫu số |
| C056 | Checksum đúng nhưng registry timeout | gtin_registry=UNKNOWN | Không đánh CONFIRMED hoặc NOT_FOUND |
| C057 | TNVED nhập nhầm, hàng thật và biến thể không đổi | Có thể metadata correction; giữ định danh chờ quy trình sửa phù hợp | Không bắt buộc cấp GTIN mới cho mọi sai lệch |
| C058 | Ngoại lệ tồn kho rút gọn chưa có căn cứ | LEGACY_EXCEPTION_UNVERIFIED; giữ chờ xử lý | Không tự hợp thức hóa GTIN chung |
| C059 | WB thay đổi thẻ sau khi lập patch | PATCH_STALE; đọc lại và đánh giá lại | Không ghi đè snapshot cũ |
| C060 | HTTP 200 nhưng đọc lại field không đổi hoặc có lỗi nghiệp vụ | PENDING_EXTERNAL/FAILED_EXTERNAL theo kết quả thực; không VERIFIED_EXTERNAL | Không báo đã sửa chỉ từ 200 |
| C061 | WB đã đổi; NC đang chờ sửa qua hỗ trợ | PARTIALLY_SYNCED | Không báo hoàn tất đồng bộ |
| C062 | Job bị timeout sau gửi và chạy lại | OUTCOME_UNKNOWN rồi reconcile; không gửi lặp mù | Không thêm barcode/GTIN hoặc tạo yêu cầu trùng |
| C063 | Danh mục pháp lý/snapshot bị hết hạn theo policy hoặc rule chưa compile | SOURCES_STALE/RULE_NOT_COMPILED; chặn auto | Không dùng ngày 07.10.2026 như hiệu lực vô hạn |
| C064 | Đã biết shop ngoài Nga, đang xét riêng rule thông báo WB cho shop Nga | applicability=OUTSIDE_CONTEXT cho rule đó; thiếu ngữ cảnh mới là UNKNOWN | Không suy nghĩa vụ của shop này chỉ từ thông báo dành cho shop Nga |
| C065 | Áo thuộc 6110 nhưng chưa loại nhánh nhẹ mỏng cổ kín | NEEDS_DATA; giữ các family cạnh tranh | Không lọc còn KNIT_TOP_REGULAR rồi chốt T004–T007 |
| C066 | Mũ có lưỡi trai, ghép từ dải vật liệu dệt | Xét 6504 ngoài ma trận; không tự áp T098 | Không chọn 6505003000 chỉ từ tên mũ và chất liệu |
| C067 | Hoodie chỉ khai 100% химические; MVP chưa có resolver hội tụ | NEEDS_DATA/ứng viên; không coi là PURE_CLASS | Không tự đổi thành SYNTHETIC hoặc giả lập tỷ lệ |
| C068 | Quần có nhánh synthetic/artificial riêng, input chỉ химические | NEEDS_DATA chi tiết sợi | Không dùng nhánh synthetic mặc định |
| C069 | Fact.value lưu cotton nhưng Fact.status=CONFLICT với chứng cứ synthetic | CONFLICT; không có tự sửa | Không dùng.value bỏ qua cờ mâu thuẫn |
| C070 | File nhập tự khai VERIFIED nhưng không có evidence/tác nhân xác nhận | EVIDENCE_ATTESTATION_INVALID; không nâng grade | Không nhận quyền xác minh từ chuỗi trong file |
| C071 | Babycotton dệt kim phủ tráng 5903, miền 61 và bộ phận phân loại đã xác minh đủ | Ưu tiên 6111; có thể T085 nếu mọi điều kiện còn lại đạt | Không chặn baby toàn cục chỉ vì phủ tráng |
| C072 | Cùng cấu tạo phủ tráng đã xác minh nhưng trẻ lớn hơn 86 cm | Xét 6113; có thể NOT_COVERED trong 104 dòng | Không bỏ qua ưu tiên vải đặc biệt để lấy mã quần áo thường |

Ngoài các ca trên, mỗi rule được bật chạy quyết định cần ít nhất một ca tích cực đầy đủ và một ca gần ranh giới nhưng bị loại bằng đúng điều kiện pháp lý. Dùng nguồn độc lập để xác nhận kỳ vọng; không sinh expected từ chính bảng mapping rồi gọi đó là kiểm chứng.

Kiểm tra bất biến: thay thứ tự dòng nhập không đổi kết luận; raw không bị ghi đè; UNKNOWN không tự biến thành FALSE/OTHER; một lần audit không sửa nguồn; chạy lại patch đã áp dụng không tạo thay đổi lặp; tenant khác không dùng nhầm snapshot/ID; phương án sửa không thay field ngoài danh sách được cấp quyền. Kiểm tải theo quy mô sản phẩm thực tế, không bỏ phân trang để chạy nhanh.

## 15 Biên dịch ma trận thành rule có kiểu dữ liệu

Ma trận JSON ở phần 16 là **conditional reference seed**. Các câu `conditions_vi` chưa phải predicate có thể chạy an toàn bằng máy. Antigravity phải chuyển chúng thành enum, phép so sánh và điều kiện loại trừ rõ ràng, kèm test và nguồn. Không chạy regex trên prose hoặc cho LLM quyết định có khớp câu chữ tại mỗi lần audit.

Mỗi row giữ nguyên rule_id, mã, nguồn và phạm vi. Không tạo một rule khác chỉ vì thay cách trình bày. Nếu phát hiện giới hạn/sai khác cần thay, ghi revision và lý do có nguồn; không âm thầm sửa mã seed.

Ví dụ cấu trúc rule nội bộ cho T013:

```json
{
  "rule_id": "T013",
  "revision": 1,
  "tnved10": "6103420001",
  "competition_group": "APPAREL_TEXTILE",
  "compile_status": "EXAMPLE_REQUIRES_IMPLEMENTATION_AND_TESTS",
  "source_ids": ["S02", "S04"],
  "all": [
    {"field": "construction", "op": "EQ", "value": "KNIT"},
    {"field": "height_scope", "op": "EQ", "value": "NON_BABY"},
    {"field": "tariff_gender", "op": "EQ", "value": "M"},
    {"field": "material_resolution", "op": "EQ", "value": "COTTON"},
    {"field": "product_form", "op": "IN", "value": ["TROUSERS", "BREECHES"]},
    {"field": "retail_classification", "op": "EQ", "value": "SEPARATE_GARMENT"},
    {"field": "intended_use", "op": "EQ", "value": "ORDINARY_APPAREL"},
    {"field": "common_apparel_scope_clear", "op": "EQ", "value": true},
    {"field": "special_heading_priority_clear", "op": "EQ", "value": true}
  ],
  "local_semantic_auto_policy": "ELIGIBLE_IF_ALL_GATES",
  "on_unknown": "NEEDS_DATA"
}
```

Đây là hình dạng DSL để triển khai, không phải payload WB hay rule đã được phát hành. `common_apparel_scope_clear` và `special_heading_priority_clear` là macro engine phải mở rộng thành predicate đã xác minh: miền sản phẩm, hàng mới/đã sử dụng, vải đặc biệt, loại trừ 6212, ưu tiên baby, trường hợp bảo hộ/y tế và những ngoại lệ làm đổi vị trí. Macro không được nhập như cờ TRUE do người dùng/AI tùy ý đặt.

`F_FALLBACK` thỏa điều kiện nhánh F của rule sau khi resolver giới tính xác nhận; nó vẫn được giữ trong trace, không sửa thành commercial FEMALE. `ANY` trong rule nghĩa rule không hạn chế nhánh giới tính; không gán ANY/UNISEX cho chính thuộc tính thương mại.

Các từ “khác”, “прочие”, hoặc “không tách theo sợi” có ý nghĩa khác nhau. “Khác” cần chứng minh đã loại các nhánh trước. “Không tách theo sợi” chỉ miễn phép chọn giữa các nhánh sợi tại mã đó; vẫn cần đúng miền vật liệu và loại hàng, ví dụ mũ dệt kim phải là mũ vật liệu dệt.

Trạng thái compiler: `UNCOMPILED` → `TESTED` → `ENABLED`. Chỉ bộ rule ENABLED, đủ kiểm tra hồi quy và đúng phiên bản nguồn được tham gia sửa tự động. Khi một rule liên quan chưa compile và vẫn có thể thay đổi kết quả, trả `RULE_NOT_COMPILED` thay vì bỏ qua nó để ép ra một đáp án.

## 16 Ma trận tham chiếu đầy đủ 104 dòng

Khối JSON sau có marker `MATRIX_TNVED_104_V1` để Antigravity trích thành dữ liệu nội bộ. Có đúng 104 rule_id T001–T104. Các mã giữ nguyên bộ mã của workbook đã tạo; diễn giải giới tính T075–T076 và phạm vi baby của phụ kiện đã được tách rõ trong đặc tả này.

Quy ước:

- `tariff_gender=M/F/ANY` là ràng buộc nhánh thuế quan, không phải giá trị giới tính thương mại bắt buộc.
- `height_scope=NON_BABY` yêu cầu đã loại trường hợp baby của Chương 61/62; `NOT_APPLICABLE` không áp ngưỡng đó.
- `local_semantic_auto_policy` chỉ là chính sách trần; không cho phép bỏ qua gate, bằng chứng, compiler, mã lá hoặc quyền thực hiện.
- `pdf_page_1_based` là số trang PDF đếm từ 1, không phải số trang in của tài liệu.
- Mã 4202 trong bảng chưa được bộ rule này kết luận nghĩa vụ маркировка; phải tra phạm vi riêng.

Hash `rows_canonical_sha256` được tính riêng trên mảng rows: JSON UTF-8, khóa của từng object sắp theo tên, không khoảng trắng ngoài chuỗi, giữ Unicode nguyên dạng; tương đương Python json.dumps(rows, ensure_ascii=False, separators=(",", ":"), sort_keys=True). Hash dùng kiểm toàn vẹn bản bàn giao, không chứng minh mã có hiệu lực pháp lý.

```json
{
  "marker": "MATRIX_TNVED_104_V1",
  "schema_version": "1.0",
  "rulepack_version": "TNVED-2026-10-07-v1",
  "reviewed_at": "2026-10-07",
  "purpose": "CONDITIONAL_REFERENCE_SEED",
  "production_executable": false,
  "row_count": 104,
  "rows_canonical_sha256": "7c35fb4ccda8e67aa984e9e0c0ec8736054fd26c69466150e18021e790193a76",
  "rows": [
    {"rule_id":"T001","family":"TSHIRT","tnved10":"6109100000","competition_group":"APPAREL_TEXTILE","tariff_gender":"ANY","height_scope":"NON_BABY","construction_vi_ru":"Dệt kim / трикотаж","product_vi":"Áo phông, áo thun, áo ba lỗ kiểu T-shirt","material_condition_vi":"Cotton / хлопок — sau khi áp dụng quy tắc hỗn hợp","conditions_vi":"Kiểu 6109; không cổ mở cài, không dây rút/bo đàn hồi/siết tại gấu. Áo polo và sweatshirt phải xét riêng.","gate_profile":"TSHIRT","local_semantic_auto_policy":"ELIGIBLE_IF_ALL_GATES","source_id":"S02","pdf_page_1_based":12},
    {"rule_id":"T002","family":"TSHIRT","tnved10":"6109902000","competition_group":"APPAREL_TEXTILE","tariff_gender":"ANY","height_scope":"NON_BABY","construction_vi_ru":"Dệt kim / трикотаж","product_vi":"Áo phông, áo thun, áo ba lỗ kiểu T-shirt","material_condition_vi":"Sợi hóa học / химические: tổng hợp hoặc nhân tạo; hoặc len/lông động vật mịn","conditions_vi":"Kiểu 6109; không cổ mở cài, không dây rút/bo đàn hồi/siết tại gấu. Áo polo và sweatshirt phải xét riêng.","gate_profile":"TSHIRT","local_semantic_auto_policy":"ELIGIBLE_IF_ALL_GATES","source_id":"S02","pdf_page_1_based":12},
    {"rule_id":"T003","family":"TSHIRT","tnved10":"6109909000","competition_group":"APPAREL_TEXTILE","tariff_gender":"ANY","height_scope":"NON_BABY","construction_vi_ru":"Dệt kim / трикотаж","product_vi":"Áo phông, áo thun, áo ba lỗ kiểu T-shirt","material_condition_vi":"Vật liệu khác; ví dụ lanh hoặc tơ tằm","conditions_vi":"Kiểu 6109; không cổ mở cài, không dây rút/bo đàn hồi/siết tại gấu. Áo polo và sweatshirt phải xét riêng.","gate_profile":"TSHIRT","local_semantic_auto_policy":"ELIGIBLE_IF_ALL_GATES","source_id":"S02","pdf_page_1_based":12},
    {"rule_id":"T004","family":"KNIT_TOP_REGULAR","tnved10":"6110209100","competition_group":"APPAREL_TEXTILE","tariff_gender":"M","height_scope":"NON_BABY","construction_vi_ru":"Dệt kim / трикотаж","product_vi":"Hoodie, sweatshirt, áo nỉ, sweater/cardigan thường","material_condition_vi":"Cotton / хлопок — sau khi áp dụng quy tắc hỗn hợp","conditions_vi":"Phải là kiểu 6110; không áo khoác ngoài 6101/6102; loại nhẹ, mỏng cổ polo/cổ cao thuộc nhánh 6110…1000. Có/không nỉ bên trong không tự đổi mã.","gate_profile":"KNIT_TOP_REGULAR","local_semantic_auto_policy":"ELIGIBLE_IF_ALL_GATES","source_id":"S02","pdf_page_1_based":13,"supporting_source_id":"S07","supporting_pdf_page_1_based":"tr8–9: sweatshirt, cardigan, các loại trừ"},
    {"rule_id":"T005","family":"KNIT_TOP_REGULAR","tnved10":"6110209900","competition_group":"APPAREL_TEXTILE","tariff_gender":"F","height_scope":"NON_BABY","construction_vi_ru":"Dệt kim / трикотаж","product_vi":"Hoodie, sweatshirt, áo nỉ, sweater/cardigan thường","material_condition_vi":"Cotton / хлопок — sau khi áp dụng quy tắc hỗn hợp","conditions_vi":"Phải là kiểu 6110; không áo khoác ngoài 6101/6102; loại nhẹ, mỏng cổ polo/cổ cao thuộc nhánh 6110…1000. Có/không nỉ bên trong không tự đổi mã.","gate_profile":"KNIT_TOP_REGULAR","local_semantic_auto_policy":"ELIGIBLE_IF_ALL_GATES","source_id":"S02","pdf_page_1_based":13,"supporting_source_id":"S07","supporting_pdf_page_1_based":"tr8–9: sweatshirt, cardigan, các loại trừ"},
    {"rule_id":"T006","family":"KNIT_TOP_REGULAR","tnved10":"6110309100","competition_group":"APPAREL_TEXTILE","tariff_gender":"M","height_scope":"NON_BABY","construction_vi_ru":"Dệt kim / трикотаж","product_vi":"Hoodie, sweatshirt, áo nỉ, sweater/cardigan thường","material_condition_vi":"Sợi hóa học / химические: tổng hợp hoặc nhân tạo","conditions_vi":"Phải là kiểu 6110; không áo khoác ngoài 6101/6102; loại nhẹ, mỏng cổ polo/cổ cao thuộc nhánh 6110…1000. Có/không nỉ bên trong không tự đổi mã.","gate_profile":"KNIT_TOP_REGULAR","local_semantic_auto_policy":"ELIGIBLE_IF_ALL_GATES","source_id":"S02","pdf_page_1_based":13,"supporting_source_id":"S07","supporting_pdf_page_1_based":"tr8–9: sweatshirt, cardigan, các loại trừ"},
    {"rule_id":"T007","family":"KNIT_TOP_REGULAR","tnved10":"6110309900","competition_group":"APPAREL_TEXTILE","tariff_gender":"F","height_scope":"NON_BABY","construction_vi_ru":"Dệt kim / трикотаж","product_vi":"Hoodie, sweatshirt, áo nỉ, sweater/cardigan thường","material_condition_vi":"Sợi hóa học / химические: tổng hợp hoặc nhân tạo","conditions_vi":"Phải là kiểu 6110; không áo khoác ngoài 6101/6102; loại nhẹ, mỏng cổ polo/cổ cao thuộc nhánh 6110…1000. Có/không nỉ bên trong không tự đổi mã.","gate_profile":"KNIT_TOP_REGULAR","local_semantic_auto_policy":"ELIGIBLE_IF_ALL_GATES","source_id":"S02","pdf_page_1_based":13,"supporting_source_id":"S07","supporting_pdf_page_1_based":"tr8–9: sweatshirt, cardigan, các loại trừ"},
    {"rule_id":"T008","family":"KNIT_TOP_FINE_NECK","tnved10":"6110201000","competition_group":"APPAREL_TEXTILE","tariff_gender":"ANY","height_scope":"NON_BABY","construction_vi_ru":"Dệt kim / трикотаж","product_vi":"Áo dệt kim nhẹ mỏng cổ polo/cổ cao","material_condition_vi":"Cotton / хлопок — sau khi áp dụng quy tắc hỗn hợp","conditions_vi":"Mỏng: >=12 vòng/cm cả hai hướng trên mẫu 10×10cm; dáng ôm phần thân trên, cổ polo/cổ cao kín không xẻ. Không đồng nhất với mọi áo polo có cài khuy.","gate_profile":"KNIT_TOP_FINE_NECK","local_semantic_auto_policy":"REVIEW_REQUIRED","source_id":"S02","pdf_page_1_based":13,"supporting_source_id":"S07","supporting_pdf_page_1_based":"tr9 (6110201000 định nghĩa12vòng/cm), tr11 (6110301000 dẫn cùng quy tắc)"},
    {"rule_id":"T009","family":"KNIT_TOP_FINE_NECK","tnved10":"6110301000","competition_group":"APPAREL_TEXTILE","tariff_gender":"ANY","height_scope":"NON_BABY","construction_vi_ru":"Dệt kim / трикотаж","product_vi":"Áo dệt kim nhẹ mỏng cổ polo/cổ cao","material_condition_vi":"Sợi hóa học / химические: tổng hợp hoặc nhân tạo","conditions_vi":"Mỏng: >=12 vòng/cm cả hai hướng trên mẫu 10×10cm; dáng ôm phần thân trên, cổ polo/cổ cao kín không xẻ. Không đồng nhất với mọi áo polo có cài khuy.","gate_profile":"KNIT_TOP_FINE_NECK","local_semantic_auto_policy":"REVIEW_REQUIRED","source_id":"S02","pdf_page_1_based":13,"supporting_source_id":"S07","supporting_pdf_page_1_based":"tr9 (6110201000 định nghĩa12vòng/cm), tr11 (6110301000 dẫn cùng quy tắc)"},
    {"rule_id":"T010","family":"WOOL_TOP_REGULAR","tnved10":"6110113000","competition_group":"APPAREL_TEXTILE","tariff_gender":"M","height_scope":"NON_BABY","construction_vi_ru":"Dệt kim / трикотаж","product_vi":"Sweater/cardigan len loại thường","material_condition_vi":"Len / шерсть; không gồm cashmere hoặc lông động vật mịn khác","conditions_vi":"Loại khác, không sweater/pullover đồng thời >=600g/chiếc và >=50% khối lượng len.","gate_profile":"WOOL_TOP_REGULAR","local_semantic_auto_policy":"REVIEW_REQUIRED","source_id":"S02","pdf_page_1_based":12},
    {"rule_id":"T011","family":"WOOL_TOP_REGULAR","tnved10":"6110119000","competition_group":"APPAREL_TEXTILE","tariff_gender":"F","height_scope":"NON_BABY","construction_vi_ru":"Dệt kim / трикотаж","product_vi":"Sweater/cardigan len loại thường","material_condition_vi":"Len / шерсть; không gồm cashmere hoặc lông động vật mịn khác","conditions_vi":"Loại khác, không sweater/pullover đồng thời >=600g/chiếc và >=50% khối lượng len.","gate_profile":"WOOL_TOP_REGULAR","local_semantic_auto_policy":"REVIEW_REQUIRED","source_id":"S02","pdf_page_1_based":12},
    {"rule_id":"T012","family":"WOOL_PULLOVER_HEAVY","tnved10":"6110111000","competition_group":"APPAREL_TEXTILE","tariff_gender":"ANY","height_scope":"NON_BABY","construction_vi_ru":"Dệt kim / трикотаж","product_vi":"Sweater/pullover len nặng từ 600 g","material_condition_vi":"Len / шерсть; không gồm cashmere hoặc lông động vật mịn khác","conditions_vi":"Chỉ sweater hoặc pullover; khối lượng >=600g/chiếc VÀ ít nhất 50% len. Không tự dùng cho cardigan.","gate_profile":"WOOL_PULLOVER_HEAVY","local_semantic_auto_policy":"REVIEW_REQUIRED","source_id":"S02","pdf_page_1_based":12},
    {"rule_id":"T013","family":"KNIT_BOTTOM","tnved10":"6103420001","competition_group":"APPAREL_TEXTILE","tariff_gender":"M","height_scope":"NON_BABY","construction_vi_ru":"Dệt kim / трикотаж","product_vi":"Quần dài, jogger, quần thể thao, quần lửng dệt kim","material_condition_vi":"Cotton / хлопок — sau khi áp dụng quy tắc hỗn hợp","conditions_vi":"Bán riêng; không short, đồ bơi, quần lót, bộ trượt tuyết hoặc bộ thể thao hoàn chỉnh. Bo chun không quyết định dệt kim.","gate_profile":"KNIT_BOTTOM","local_semantic_auto_policy":"ELIGIBLE_IF_ALL_GATES","source_id":"S02","pdf_page_1_based":7},
    {"rule_id":"T014","family":"KNIT_BOTTOM","tnved10":"6103430001","competition_group":"APPAREL_TEXTILE","tariff_gender":"M","height_scope":"NON_BABY","construction_vi_ru":"Dệt kim / трикотаж","product_vi":"Quần dài, jogger, quần thể thao, quần lửng dệt kim","material_condition_vi":"Sợi tổng hợp / синтетические: polyester, polyamide, elastane…","conditions_vi":"Bán riêng; không short, đồ bơi, quần lót, bộ trượt tuyết hoặc bộ thể thao hoàn chỉnh. Bo chun không quyết định dệt kim.","gate_profile":"KNIT_BOTTOM","local_semantic_auto_policy":"ELIGIBLE_IF_ALL_GATES","source_id":"S02","pdf_page_1_based":7},
    {"rule_id":"T015","family":"KNIT_BOTTOM","tnved10":"6103490001","competition_group":"APPAREL_TEXTILE","tariff_gender":"M","height_scope":"NON_BABY","construction_vi_ru":"Dệt kim / трикотаж","product_vi":"Quần dài, jogger, quần thể thao, quần lửng dệt kim","material_condition_vi":"Vật liệu khác; ví dụ viscose; không len nhánh 610341","conditions_vi":"Bán riêng; không short, đồ bơi, quần lót, bộ trượt tuyết hoặc bộ thể thao hoàn chỉnh. Bo chun không quyết định dệt kim.","gate_profile":"KNIT_BOTTOM","local_semantic_auto_policy":"ELIGIBLE_IF_ALL_GATES","source_id":"S02","pdf_page_1_based":7},
    {"rule_id":"T016","family":"KNIT_BOTTOM","tnved10":"6103420009","competition_group":"APPAREL_TEXTILE","tariff_gender":"M","height_scope":"NON_BABY","construction_vi_ru":"Dệt kim / трикотаж","product_vi":"Quần short dệt kim","material_condition_vi":"Cotton / хлопок — sau khi áp dụng quy tắc hỗn hợp","conditions_vi":"Short thường, không đồ bơi/quần lót. Mã nhánh này còn gồm quần yếm có ngực và dây; không dùng mã quần dài đuôi 1.","gate_profile":"KNIT_BOTTOM","local_semantic_auto_policy":"ELIGIBLE_IF_ALL_GATES","source_id":"S02","pdf_page_1_based":7},
    {"rule_id":"T017","family":"KNIT_BOTTOM","tnved10":"6103430009","competition_group":"APPAREL_TEXTILE","tariff_gender":"M","height_scope":"NON_BABY","construction_vi_ru":"Dệt kim / трикотаж","product_vi":"Quần short dệt kim","material_condition_vi":"Sợi tổng hợp / синтетические: polyester, polyamide, elastane…","conditions_vi":"Short thường, không đồ bơi/quần lót. Mã nhánh này còn gồm quần yếm có ngực và dây; không dùng mã quần dài đuôi 1.","gate_profile":"KNIT_BOTTOM","local_semantic_auto_policy":"ELIGIBLE_IF_ALL_GATES","source_id":"S02","pdf_page_1_based":7},
    {"rule_id":"T018","family":"KNIT_BOTTOM","tnved10":"6103490002","competition_group":"APPAREL_TEXTILE","tariff_gender":"M","height_scope":"NON_BABY","construction_vi_ru":"Dệt kim / трикотаж","product_vi":"Quần short dệt kim","material_condition_vi":"Sợi nhân tạo / искусственные: viscose, modal, lyocell, acetate…","conditions_vi":"Short thường, không đồ bơi/quần lót. Mã nhánh này còn gồm quần yếm có ngực và dây; không dùng mã quần dài đuôi 1.","gate_profile":"KNIT_BOTTOM","local_semantic_auto_policy":"ELIGIBLE_IF_ALL_GATES","source_id":"S02","pdf_page_1_based":7},
    {"rule_id":"T019","family":"KNIT_BOTTOM","tnved10":"6104620000","competition_group":"APPAREL_TEXTILE","tariff_gender":"F","height_scope":"NON_BABY","construction_vi_ru":"Dệt kim / трикотаж","product_vi":"Quần dài, jogger, quần lửng hoặc short dệt kim","material_condition_vi":"Cotton / хлопок — sau khi áp dụng quy tắc hỗn hợp","conditions_vi":"Bao gồm quần yếm có ngực và dây. Không đồ bơi/quần lót/bộ trượt tuyết; nữ cotton/tổng hợp không tách đuôi giữa dài và short.","gate_profile":"KNIT_BOTTOM","local_semantic_auto_policy":"ELIGIBLE_IF_ALL_GATES","source_id":"S02","pdf_page_1_based":9},
    {"rule_id":"T020","family":"KNIT_BOTTOM","tnved10":"6104630000","competition_group":"APPAREL_TEXTILE","tariff_gender":"F","height_scope":"NON_BABY","construction_vi_ru":"Dệt kim / трикотаж","product_vi":"Quần dài, jogger, quần lửng hoặc short dệt kim","material_condition_vi":"Sợi tổng hợp / синтетические: polyester, polyamide, elastane…","conditions_vi":"Bao gồm quần yếm có ngực và dây. Không đồ bơi/quần lót/bộ trượt tuyết; nữ cotton/tổng hợp không tách đuôi giữa dài và short.","gate_profile":"KNIT_BOTTOM","local_semantic_auto_policy":"ELIGIBLE_IF_ALL_GATES","source_id":"S02","pdf_page_1_based":9},
    {"rule_id":"T021","family":"KNIT_BOTTOM","tnved10":"6104690001","competition_group":"APPAREL_TEXTILE","tariff_gender":"F","height_scope":"NON_BABY","construction_vi_ru":"Dệt kim / трикотаж","product_vi":"Quần dài/lửng dệt kim viscose hoặc vật liệu khác","material_condition_vi":"Vật liệu khác; ví dụ viscose; không len nhánh 610461","conditions_vi":"Không short/quần yếm; không cotton hoặc sợi tổng hợp.","gate_profile":"KNIT_BOTTOM","local_semantic_auto_policy":"ELIGIBLE_IF_ALL_GATES","source_id":"S02","pdf_page_1_based":9},
    {"rule_id":"T022","family":"KNIT_BOTTOM","tnved10":"6104690002","competition_group":"APPAREL_TEXTILE","tariff_gender":"F","height_scope":"NON_BABY","construction_vi_ru":"Dệt kim / трикотаж","product_vi":"Quần short dệt kim viscose","material_condition_vi":"Sợi nhân tạo / искусственные: viscose, modal, lyocell, acetate…","conditions_vi":"Short thường, không đồ bơi/quần lót; nhánh cũng bao gồm quần yếm có ngực và dây.","gate_profile":"KNIT_BOTTOM","local_semantic_auto_policy":"ELIGIBLE_IF_ALL_GATES","source_id":"S02","pdf_page_1_based":9},
    {"rule_id":"T023","family":"WOVEN_TROUSER","tnved10":"6203423100","competition_group":"APPAREL_TEXTILE","tariff_gender":"M","height_scope":"NON_BABY","construction_vi_ru":"Dệt thoi / нетрикотаж","product_vi":"Quần jeans","material_condition_vi":"Cotton / хлопок — sau khi áp dụng quy tắc hỗn hợp","conditions_vi":"Hàng thường, không quần lao động/bảo hộ, không short, không yếm có ngực và dây. Vải denim thật; kiểu baggy/flare/straight/cropped không đổi mã khi các điều kiện khác giống nhau.","gate_profile":"WOVEN_TROUSER","local_semantic_auto_policy":"ELIGIBLE_IF_ALL_GATES","source_id":"S03","pdf_page_1_based":7},
    {"rule_id":"T024","family":"WOVEN_TROUSER","tnved10":"6203423300","competition_group":"APPAREL_TEXTILE","tariff_gender":"M","height_scope":"NON_BABY","construction_vi_ru":"Dệt thoi / нетрикотаж","product_vi":"Quần nhung gân","material_condition_vi":"Cotton / хлопок — sau khi áp dụng quy tắc hỗn hợp","conditions_vi":"Hàng thường, không quần lao động/bảo hộ, không short, không yếm có ngực và dây. Vải вельвет-корд có lông cắt.","gate_profile":"WOVEN_TROUSER","local_semantic_auto_policy":"ELIGIBLE_IF_ALL_GATES","source_id":"S03","pdf_page_1_based":7},
    {"rule_id":"T025","family":"WOVEN_TROUSER","tnved10":"6203423500","competition_group":"APPAREL_TEXTILE","tariff_gender":"M","height_scope":"NON_BABY","construction_vi_ru":"Dệt thoi / нетрикотаж","product_vi":"Quần dài/lửng cotton khác, cargo","material_condition_vi":"Cotton / хлопок — sau khi áp dụng quy tắc hỗn hợp","conditions_vi":"Hàng thường, không quần lao động/bảo hộ, không short, không yếm có ngực và dây. Không denim hoặc вельвет-корд có lông cắt.","gate_profile":"WOVEN_TROUSER","local_semantic_auto_policy":"ELIGIBLE_IF_ALL_GATES","source_id":"S03","pdf_page_1_based":7},
    {"rule_id":"T026","family":"WOVEN_TROUSER","tnved10":"6203431900","competition_group":"APPAREL_TEXTILE","tariff_gender":"M","height_scope":"NON_BABY","construction_vi_ru":"Dệt thoi / нетрикотаж","product_vi":"Quần dài/lửng tổng hợp, cargo","material_condition_vi":"Sợi tổng hợp / синтетические: polyester, polyamide, elastane…","conditions_vi":"Hàng thường, không quần lao động/bảo hộ, không short, không yếm có ngực và dây.","gate_profile":"WOVEN_TROUSER","local_semantic_auto_policy":"ELIGIBLE_IF_ALL_GATES","source_id":"S03","pdf_page_1_based":8},
    {"rule_id":"T027","family":"WOVEN_TROUSER","tnved10":"6203491900","competition_group":"APPAREL_TEXTILE","tariff_gender":"M","height_scope":"NON_BABY","construction_vi_ru":"Dệt thoi / нетрикотаж","product_vi":"Quần dài/lửng viscose","material_condition_vi":"Sợi nhân tạo / искусственные: viscose, modal, lyocell, acetate…","conditions_vi":"Hàng thường, không quần lao động/bảo hộ, không short, không yếm có ngực và dây.","gate_profile":"WOVEN_TROUSER","local_semantic_auto_policy":"ELIGIBLE_IF_ALL_GATES","source_id":"S03","pdf_page_1_based":8},
    {"rule_id":"T028","family":"WOVEN_TROUSER","tnved10":"6204623100","competition_group":"APPAREL_TEXTILE","tariff_gender":"F","height_scope":"NON_BABY","construction_vi_ru":"Dệt thoi / нетрикотаж","product_vi":"Quần jeans","material_condition_vi":"Cotton / хлопок — sau khi áp dụng quy tắc hỗn hợp","conditions_vi":"Hàng thường, không quần lao động/bảo hộ, không short, không yếm có ngực và dây. Vải denim thật; kiểu baggy/flare/straight/cropped không đổi mã khi các điều kiện khác giống nhau.","gate_profile":"WOVEN_TROUSER","local_semantic_auto_policy":"ELIGIBLE_IF_ALL_GATES","source_id":"S03","pdf_page_1_based":11},
    {"rule_id":"T029","family":"WOVEN_TROUSER","tnved10":"6204623300","competition_group":"APPAREL_TEXTILE","tariff_gender":"F","height_scope":"NON_BABY","construction_vi_ru":"Dệt thoi / нетрикотаж","product_vi":"Quần nhung gân","material_condition_vi":"Cotton / хлопок — sau khi áp dụng quy tắc hỗn hợp","conditions_vi":"Hàng thường, không quần lao động/bảo hộ, không short, không yếm có ngực và dây. Vải вельвет-корд có lông cắt.","gate_profile":"WOVEN_TROUSER","local_semantic_auto_policy":"ELIGIBLE_IF_ALL_GATES","source_id":"S03","pdf_page_1_based":11},
    {"rule_id":"T030","family":"WOVEN_TROUSER","tnved10":"6204623900","competition_group":"APPAREL_TEXTILE","tariff_gender":"F","height_scope":"NON_BABY","construction_vi_ru":"Dệt thoi / нетрикотаж","product_vi":"Quần dài/lửng cotton khác, cargo","material_condition_vi":"Cotton / хлопок — sau khi áp dụng quy tắc hỗn hợp","conditions_vi":"Hàng thường, không quần lao động/bảo hộ, không short, không yếm có ngực và dây. Không denim hoặc вельвет-корд có lông cắt.","gate_profile":"WOVEN_TROUSER","local_semantic_auto_policy":"ELIGIBLE_IF_ALL_GATES","source_id":"S03","pdf_page_1_based":11},
    {"rule_id":"T031","family":"WOVEN_TROUSER","tnved10":"6204631800","competition_group":"APPAREL_TEXTILE","tariff_gender":"F","height_scope":"NON_BABY","construction_vi_ru":"Dệt thoi / нетрикотаж","product_vi":"Quần dài/lửng tổng hợp, cargo","material_condition_vi":"Sợi tổng hợp / синтетические: polyester, polyamide, elastane…","conditions_vi":"Hàng thường, không quần lao động/bảo hộ, không short, không yếm có ngực và dây.","gate_profile":"WOVEN_TROUSER","local_semantic_auto_policy":"ELIGIBLE_IF_ALL_GATES","source_id":"S03","pdf_page_1_based":11},
    {"rule_id":"T032","family":"WOVEN_TROUSER","tnved10":"6204691800","competition_group":"APPAREL_TEXTILE","tariff_gender":"F","height_scope":"NON_BABY","construction_vi_ru":"Dệt thoi / нетрикотаж","product_vi":"Quần dài/lửng viscose","material_condition_vi":"Sợi nhân tạo / искусственные: viscose, modal, lyocell, acetate…","conditions_vi":"Hàng thường, không quần lao động/bảo hộ, không short, không yếm có ngực và dây.","gate_profile":"WOVEN_TROUSER","local_semantic_auto_policy":"ELIGIBLE_IF_ALL_GATES","source_id":"S03","pdf_page_1_based":12},
    {"rule_id":"T033","family":"WOVEN_SHORT","tnved10":"6203429000","competition_group":"APPAREL_TEXTILE","tariff_gender":"M","height_scope":"NON_BABY","construction_vi_ru":"Dệt thoi / нетрикотаж","product_vi":"Quần short dệt thoi","material_condition_vi":"Cotton / хлопок — sau khi áp dụng quy tắc hỗn hợp","conditions_vi":"Không đồ bơi/quần lót/quần dài/quần yếm có ngực và dây.","gate_profile":"WOVEN_SHORT","local_semantic_auto_policy":"ELIGIBLE_IF_ALL_GATES","source_id":"S03","pdf_page_1_based":7},
    {"rule_id":"T034","family":"WOVEN_SHORT","tnved10":"6203439000","competition_group":"APPAREL_TEXTILE","tariff_gender":"M","height_scope":"NON_BABY","construction_vi_ru":"Dệt thoi / нетрикотаж","product_vi":"Quần short dệt thoi","material_condition_vi":"Sợi tổng hợp / синтетические: polyester, polyamide, elastane…","conditions_vi":"Không đồ bơi/quần lót/quần dài/quần yếm có ngực và dây.","gate_profile":"WOVEN_SHORT","local_semantic_auto_policy":"ELIGIBLE_IF_ALL_GATES","source_id":"S03","pdf_page_1_based":8},
    {"rule_id":"T035","family":"WOVEN_SHORT","tnved10":"6203495000","competition_group":"APPAREL_TEXTILE","tariff_gender":"M","height_scope":"NON_BABY","construction_vi_ru":"Dệt thoi / нетрикотаж","product_vi":"Quần short dệt thoi","material_condition_vi":"Sợi nhân tạo / искусственные: viscose, modal, lyocell, acetate…","conditions_vi":"Không đồ bơi/quần lót/quần dài/quần yếm có ngực và dây.","gate_profile":"WOVEN_SHORT","local_semantic_auto_policy":"ELIGIBLE_IF_ALL_GATES","source_id":"S03","pdf_page_1_based":8},
    {"rule_id":"T036","family":"WOVEN_SHORT","tnved10":"6204629000","competition_group":"APPAREL_TEXTILE","tariff_gender":"F","height_scope":"NON_BABY","construction_vi_ru":"Dệt thoi / нетрикотаж","product_vi":"Quần short dệt thoi","material_condition_vi":"Cotton / хлопок — sau khi áp dụng quy tắc hỗn hợp","conditions_vi":"Không đồ bơi/quần lót/quần dài/quần yếm có ngực và dây.","gate_profile":"WOVEN_SHORT","local_semantic_auto_policy":"ELIGIBLE_IF_ALL_GATES","source_id":"S03","pdf_page_1_based":11},
    {"rule_id":"T037","family":"WOVEN_SHORT","tnved10":"6204639000","competition_group":"APPAREL_TEXTILE","tariff_gender":"F","height_scope":"NON_BABY","construction_vi_ru":"Dệt thoi / нетрикотаж","product_vi":"Quần short dệt thoi","material_condition_vi":"Sợi tổng hợp / синтетические: polyester, polyamide, elastane…","conditions_vi":"Không đồ bơi/quần lót/quần dài/quần yếm có ngực và dây.","gate_profile":"WOVEN_SHORT","local_semantic_auto_policy":"ELIGIBLE_IF_ALL_GATES","source_id":"S03","pdf_page_1_based":11},
    {"rule_id":"T038","family":"WOVEN_SHORT","tnved10":"6204695000","competition_group":"APPAREL_TEXTILE","tariff_gender":"F","height_scope":"NON_BABY","construction_vi_ru":"Dệt thoi / нетрикотаж","product_vi":"Quần short dệt thoi","material_condition_vi":"Sợi nhân tạo / искусственные: viscose, modal, lyocell, acetate…","conditions_vi":"Không đồ bơi/quần lót/quần dài/quần yếm có ngực và dây.","gate_profile":"WOVEN_SHORT","local_semantic_auto_policy":"ELIGIBLE_IF_ALL_GATES","source_id":"S03","pdf_page_1_based":12},
    {"rule_id":"T039","family":"DRESS","tnved10":"6104420000","competition_group":"APPAREL_TEXTILE","tariff_gender":"F","height_scope":"NON_BABY","construction_vi_ru":"Dệt kim / трикотаж","product_vi":"Váy liền dệt kim","material_condition_vi":"Cotton / хлопок — sau khi áp dụng quy tắc hỗn hợp","conditions_vi":"Váy liền mặc ngoài; không váy ngủ hoặc váy lót. Tên marketing сарафан/váy hai dây không đủ để phân biệt với nội y; cần kiểu sử dụng thực tế.","gate_profile":"DRESS","local_semantic_auto_policy":"ELIGIBLE_IF_ALL_GATES","source_id":"S02","pdf_page_1_based":9},
    {"rule_id":"T040","family":"DRESS","tnved10":"6104430000","competition_group":"APPAREL_TEXTILE","tariff_gender":"F","height_scope":"NON_BABY","construction_vi_ru":"Dệt kim / трикотаж","product_vi":"Váy liền dệt kim","material_condition_vi":"Sợi tổng hợp / синтетические: polyester, polyamide, elastane…","conditions_vi":"Váy liền mặc ngoài; không váy ngủ hoặc váy lót. Tên marketing сарафан/váy hai dây không đủ để phân biệt với nội y; cần kiểu sử dụng thực tế.","gate_profile":"DRESS","local_semantic_auto_policy":"ELIGIBLE_IF_ALL_GATES","source_id":"S02","pdf_page_1_based":9},
    {"rule_id":"T041","family":"DRESS","tnved10":"6104440000","competition_group":"APPAREL_TEXTILE","tariff_gender":"F","height_scope":"NON_BABY","construction_vi_ru":"Dệt kim / трикотаж","product_vi":"Váy liền dệt kim","material_condition_vi":"Sợi nhân tạo / искусственные: viscose, modal, lyocell, acetate…","conditions_vi":"Váy liền mặc ngoài; không váy ngủ hoặc váy lót. Tên marketing сарафан/váy hai dây không đủ để phân biệt với nội y; cần kiểu sử dụng thực tế.","gate_profile":"DRESS","local_semantic_auto_policy":"ELIGIBLE_IF_ALL_GATES","source_id":"S02","pdf_page_1_based":9},
    {"rule_id":"T042","family":"DRESS","tnved10":"6204420000","competition_group":"APPAREL_TEXTILE","tariff_gender":"F","height_scope":"NON_BABY","construction_vi_ru":"Dệt thoi / нетрикотаж","product_vi":"Váy liền dệt thoi","material_condition_vi":"Cotton / хлопок — sau khi áp dụng quy tắc hỗn hợp","conditions_vi":"Váy liền mặc ngoài; không váy ngủ hoặc váy lót. Tên marketing сарафан/váy hai dây không đủ để phân biệt với nội y; cần kiểu sử dụng thực tế.","gate_profile":"DRESS","local_semantic_auto_policy":"ELIGIBLE_IF_ALL_GATES","source_id":"S03","pdf_page_1_based":10},
    {"rule_id":"T043","family":"DRESS","tnved10":"6204430000","competition_group":"APPAREL_TEXTILE","tariff_gender":"F","height_scope":"NON_BABY","construction_vi_ru":"Dệt thoi / нетрикотаж","product_vi":"Váy liền dệt thoi","material_condition_vi":"Sợi tổng hợp / синтетические: polyester, polyamide, elastane…","conditions_vi":"Váy liền mặc ngoài; không váy ngủ hoặc váy lót. Tên marketing сарафан/váy hai dây không đủ để phân biệt với nội y; cần kiểu sử dụng thực tế.","gate_profile":"DRESS","local_semantic_auto_policy":"ELIGIBLE_IF_ALL_GATES","source_id":"S03","pdf_page_1_based":10},
    {"rule_id":"T044","family":"DRESS","tnved10":"6204440000","competition_group":"APPAREL_TEXTILE","tariff_gender":"F","height_scope":"NON_BABY","construction_vi_ru":"Dệt thoi / нетрикотаж","product_vi":"Váy liền dệt thoi","material_condition_vi":"Sợi nhân tạo / искусственные: viscose, modal, lyocell, acetate…","conditions_vi":"Váy liền mặc ngoài; không váy ngủ hoặc váy lót. Tên marketing сарафан/váy hai dây không đủ để phân biệt với nội y; cần kiểu sử dụng thực tế.","gate_profile":"DRESS","local_semantic_auto_policy":"ELIGIBLE_IF_ALL_GATES","source_id":"S03","pdf_page_1_based":10},
    {"rule_id":"T045","family":"SKIRT","tnved10":"6104520000","competition_group":"APPAREL_TEXTILE","tariff_gender":"F","height_scope":"NON_BABY","construction_vi_ru":"Dệt kim / трикотаж","product_vi":"Chân váy, váy quần dệt kim","material_condition_vi":"Cotton / хлопок — sau khi áp dụng quy tắc hỗn hợp","conditions_vi":"Không đồ ngủ/nội y. Chân váy jeans cotton dệt thoi vẫn theo 6204520000, không mã quần jeans.","gate_profile":"SKIRT","local_semantic_auto_policy":"ELIGIBLE_IF_ALL_GATES","source_id":"S02","pdf_page_1_based":9},
    {"rule_id":"T046","family":"SKIRT","tnved10":"6104530000","competition_group":"APPAREL_TEXTILE","tariff_gender":"F","height_scope":"NON_BABY","construction_vi_ru":"Dệt kim / трикотаж","product_vi":"Chân váy, váy quần dệt kim","material_condition_vi":"Sợi tổng hợp / синтетические: polyester, polyamide, elastane…","conditions_vi":"Không đồ ngủ/nội y. Chân váy jeans cotton dệt thoi vẫn theo 6204520000, không mã quần jeans.","gate_profile":"SKIRT","local_semantic_auto_policy":"ELIGIBLE_IF_ALL_GATES","source_id":"S02","pdf_page_1_based":9},
    {"rule_id":"T047","family":"SKIRT","tnved10":"6104590000","competition_group":"APPAREL_TEXTILE","tariff_gender":"F","height_scope":"NON_BABY","construction_vi_ru":"Dệt kim / трикотаж","product_vi":"Chân váy, váy quần dệt kim","material_condition_vi":"Vật liệu khác, gồm viscose; ngoài len/cotton/tổng hợp","conditions_vi":"Không đồ ngủ/nội y. Chân váy jeans cotton dệt thoi vẫn theo 6204520000, không mã quần jeans.","gate_profile":"SKIRT","local_semantic_auto_policy":"ELIGIBLE_IF_ALL_GATES","source_id":"S02","pdf_page_1_based":9},
    {"rule_id":"T048","family":"SKIRT","tnved10":"6204520000","competition_group":"APPAREL_TEXTILE","tariff_gender":"F","height_scope":"NON_BABY","construction_vi_ru":"Dệt thoi / нетрикотаж","product_vi":"Chân váy, váy quần dệt thoi","material_condition_vi":"Cotton / хлопок — sau khi áp dụng quy tắc hỗn hợp","conditions_vi":"Không đồ ngủ/nội y. Chân váy jeans cotton dệt thoi vẫn theo 6204520000, không mã quần jeans.","gate_profile":"SKIRT","local_semantic_auto_policy":"ELIGIBLE_IF_ALL_GATES","source_id":"S03","pdf_page_1_based":10},
    {"rule_id":"T049","family":"SKIRT","tnved10":"6204530000","competition_group":"APPAREL_TEXTILE","tariff_gender":"F","height_scope":"NON_BABY","construction_vi_ru":"Dệt thoi / нетрикотаж","product_vi":"Chân váy, váy quần dệt thoi","material_condition_vi":"Sợi tổng hợp / синтетические: polyester, polyamide, elastane…","conditions_vi":"Không đồ ngủ/nội y. Chân váy jeans cotton dệt thoi vẫn theo 6204520000, không mã quần jeans.","gate_profile":"SKIRT","local_semantic_auto_policy":"ELIGIBLE_IF_ALL_GATES","source_id":"S03","pdf_page_1_based":10},
    {"rule_id":"T050","family":"SKIRT","tnved10":"6204591000","competition_group":"APPAREL_TEXTILE","tariff_gender":"F","height_scope":"NON_BABY","construction_vi_ru":"Dệt thoi / нетрикотаж","product_vi":"Chân váy, váy quần dệt thoi","material_condition_vi":"Sợi nhân tạo / искусственные: viscose, modal, lyocell, acetate…","conditions_vi":"Không đồ ngủ/nội y. Chân váy jeans cotton dệt thoi vẫn theo 6204520000, không mã quần jeans.","gate_profile":"SKIRT","local_semantic_auto_policy":"ELIGIBLE_IF_ALL_GATES","source_id":"S03","pdf_page_1_based":10},
    {"rule_id":"T051","family":"KNIT_SHIRT","tnved10":"6105100000","competition_group":"APPAREL_TEXTILE","tariff_gender":"M","height_scope":"NON_BABY","construction_vi_ru":"Dệt kim / трикотаж","product_vi":"Sơ mi/polo","material_condition_vi":"Cotton / хлопок — sau khi áp dụng quy tắc hỗn hợp","conditions_vi":"Không túi dưới eo, không bo/dây siết gấu. Nam phải có tay; áo sơ mi có xẻ từ cổ. Dệt kim >=10 vòng/cm ở mỗi hướng, mẫu >=10×10cm. Áo nữ có thể là blouse không tay.","gate_profile":"KNIT_SHIRT","local_semantic_auto_policy":"REVIEW_REQUIRED","source_id":"S02","pdf_page_1_based":10},
    {"rule_id":"T052","family":"KNIT_SHIRT","tnved10":"6105201000","competition_group":"APPAREL_TEXTILE","tariff_gender":"M","height_scope":"NON_BABY","construction_vi_ru":"Dệt kim / трикотаж","product_vi":"Sơ mi/polo","material_condition_vi":"Sợi tổng hợp / синтетические: polyester, polyamide, elastane…","conditions_vi":"Không túi dưới eo, không bo/dây siết gấu. Nam phải có tay; áo sơ mi có xẻ từ cổ. Dệt kim >=10 vòng/cm ở mỗi hướng, mẫu >=10×10cm. Áo nữ có thể là blouse không tay.","gate_profile":"KNIT_SHIRT","local_semantic_auto_policy":"REVIEW_REQUIRED","source_id":"S02","pdf_page_1_based":10},
    {"rule_id":"T053","family":"KNIT_SHIRT","tnved10":"6105209000","competition_group":"APPAREL_TEXTILE","tariff_gender":"M","height_scope":"NON_BABY","construction_vi_ru":"Dệt kim / трикотаж","product_vi":"Sơ mi/polo","material_condition_vi":"Sợi nhân tạo / искусственные: viscose, modal, lyocell, acetate…","conditions_vi":"Không túi dưới eo, không bo/dây siết gấu. Nam phải có tay; áo sơ mi có xẻ từ cổ. Dệt kim >=10 vòng/cm ở mỗi hướng, mẫu >=10×10cm. Áo nữ có thể là blouse không tay.","gate_profile":"KNIT_SHIRT","local_semantic_auto_policy":"REVIEW_REQUIRED","source_id":"S02","pdf_page_1_based":10},
    {"rule_id":"T054","family":"KNIT_SHIRT","tnved10":"6106100000","competition_group":"APPAREL_TEXTILE","tariff_gender":"F","height_scope":"NON_BABY","construction_vi_ru":"Dệt kim / трикотаж","product_vi":"Áo blouse, sơ mi nữ","material_condition_vi":"Cotton / хлопок — sau khi áp dụng quy tắc hỗn hợp","conditions_vi":"Không túi dưới eo, không bo/dây siết gấu. Nam phải có tay; áo sơ mi có xẻ từ cổ. Dệt kim >=10 vòng/cm ở mỗi hướng, mẫu >=10×10cm. Áo nữ có thể là blouse không tay.","gate_profile":"KNIT_SHIRT","local_semantic_auto_policy":"REVIEW_REQUIRED","source_id":"S02","pdf_page_1_based":10},
    {"rule_id":"T055","family":"KNIT_SHIRT","tnved10":"6106200000","competition_group":"APPAREL_TEXTILE","tariff_gender":"F","height_scope":"NON_BABY","construction_vi_ru":"Dệt kim / трикотаж","product_vi":"Áo blouse, sơ mi nữ","material_condition_vi":"Sợi hóa học / химические: tổng hợp hoặc nhân tạo","conditions_vi":"Không túi dưới eo, không bo/dây siết gấu. Nam phải có tay; áo sơ mi có xẻ từ cổ. Dệt kim >=10 vòng/cm ở mỗi hướng, mẫu >=10×10cm. Áo nữ có thể là blouse không tay.","gate_profile":"KNIT_SHIRT","local_semantic_auto_policy":"REVIEW_REQUIRED","source_id":"S02","pdf_page_1_based":10},
    {"rule_id":"T056","family":"WOVEN_SHIRT","tnved10":"6205200000","competition_group":"APPAREL_TEXTILE","tariff_gender":"M","height_scope":"NON_BABY","construction_vi_ru":"Dệt thoi / нетрикотаж","product_vi":"Sơ mi/polo","material_condition_vi":"Cotton / хлопок — sau khi áp dụng quy tắc hỗn hợp","conditions_vi":"Không túi dưới eo, không bo/dây siết gấu. Nam phải có tay; áo sơ mi có xẻ từ cổ.","gate_profile":"WOVEN_SHIRT","local_semantic_auto_policy":"REVIEW_REQUIRED","source_id":"S03","pdf_page_1_based":12},
    {"rule_id":"T057","family":"WOVEN_SHIRT","tnved10":"6205300000","competition_group":"APPAREL_TEXTILE","tariff_gender":"M","height_scope":"NON_BABY","construction_vi_ru":"Dệt thoi / нетрикотаж","product_vi":"Sơ mi/polo","material_condition_vi":"Sợi hóa học / химические: tổng hợp hoặc nhân tạo","conditions_vi":"Không túi dưới eo, không bo/dây siết gấu. Nam phải có tay; áo sơ mi có xẻ từ cổ.","gate_profile":"WOVEN_SHIRT","local_semantic_auto_policy":"REVIEW_REQUIRED","source_id":"S03","pdf_page_1_based":12},
    {"rule_id":"T058","family":"WOVEN_SHIRT","tnved10":"6205901000","competition_group":"APPAREL_TEXTILE","tariff_gender":"M","height_scope":"NON_BABY","construction_vi_ru":"Dệt thoi / нетрикотаж","product_vi":"Sơ mi/polo","material_condition_vi":"Lanh hoặc gai ramie / лён или рами","conditions_vi":"Không túi dưới eo, không bo/dây siết gấu. Nam phải có tay; áo sơ mi có xẻ từ cổ.","gate_profile":"WOVEN_SHIRT","local_semantic_auto_policy":"REVIEW_REQUIRED","source_id":"S03","pdf_page_1_based":12},
    {"rule_id":"T059","family":"WOVEN_SHIRT","tnved10":"6206300000","competition_group":"APPAREL_TEXTILE","tariff_gender":"F","height_scope":"NON_BABY","construction_vi_ru":"Dệt thoi / нетрикотаж","product_vi":"Áo blouse, sơ mi nữ","material_condition_vi":"Cotton / хлопок — sau khi áp dụng quy tắc hỗn hợp","conditions_vi":"Không túi dưới eo, không bo/dây siết gấu. Nam phải có tay; áo sơ mi có xẻ từ cổ.","gate_profile":"WOVEN_SHIRT","local_semantic_auto_policy":"REVIEW_REQUIRED","source_id":"S03","pdf_page_1_based":12},
    {"rule_id":"T060","family":"WOVEN_SHIRT","tnved10":"6206400000","competition_group":"APPAREL_TEXTILE","tariff_gender":"F","height_scope":"NON_BABY","construction_vi_ru":"Dệt thoi / нетрикотаж","product_vi":"Áo blouse, sơ mi nữ","material_condition_vi":"Sợi hóa học / химические: tổng hợp hoặc nhân tạo","conditions_vi":"Không túi dưới eo, không bo/dây siết gấu. Nam phải có tay; áo sơ mi có xẻ từ cổ.","gate_profile":"WOVEN_SHIRT","local_semantic_auto_policy":"REVIEW_REQUIRED","source_id":"S03","pdf_page_1_based":12},
    {"rule_id":"T061","family":"WOVEN_SHIRT","tnved10":"6206100000","competition_group":"APPAREL_TEXTILE","tariff_gender":"F","height_scope":"NON_BABY","construction_vi_ru":"Dệt thoi / нетрикотаж","product_vi":"Áo blouse, sơ mi nữ","material_condition_vi":"Tơ tằm / шёлк","conditions_vi":"Không túi dưới eo, không bo/dây siết gấu. Nam phải có tay; áo sơ mi có xẻ từ cổ.","gate_profile":"WOVEN_SHIRT","local_semantic_auto_policy":"REVIEW_REQUIRED","source_id":"S03","pdf_page_1_based":12},
    {"rule_id":"T062","family":"WOVEN_SHIRT","tnved10":"6206901000","competition_group":"APPAREL_TEXTILE","tariff_gender":"F","height_scope":"NON_BABY","construction_vi_ru":"Dệt thoi / нетрикотаж","product_vi":"Áo blouse, sơ mi nữ","material_condition_vi":"Lanh hoặc gai ramie / лён или рами","conditions_vi":"Không túi dưới eo, không bo/dây siết gấu. Nam phải có tay; áo sơ mi có xẻ từ cổ.","gate_profile":"WOVEN_SHIRT","local_semantic_auto_policy":"REVIEW_REQUIRED","source_id":"S03","pdf_page_1_based":13},
    {"rule_id":"T063","family":"SLEEPWEAR","tnved10":"6107210000","competition_group":"APPAREL_TEXTILE","tariff_gender":"M","height_scope":"NON_BABY","construction_vi_ru":"Dệt kim / трикотаж","product_vi":"Bộ pyjama, áo/váy ngủ","material_condition_vi":"Cotton / хлопок — sau khi áp dụng quy tắc hỗn hợp","conditions_vi":"Phải nhận biết thực sự là đồ ngủ; bộ mặc nhà hoặc bộ T-shirt+short không tự động là pyjama. Không áp cho mọi đồ ngủ liền thân.","gate_profile":"SLEEPWEAR","local_semantic_auto_policy":"REVIEW_REQUIRED","source_id":"S02","pdf_page_1_based":11},
    {"rule_id":"T064","family":"SLEEPWEAR","tnved10":"6107220000","competition_group":"APPAREL_TEXTILE","tariff_gender":"M","height_scope":"NON_BABY","construction_vi_ru":"Dệt kim / трикотаж","product_vi":"Bộ pyjama, áo/váy ngủ","material_condition_vi":"Sợi hóa học / химические: tổng hợp hoặc nhân tạo","conditions_vi":"Phải nhận biết thực sự là đồ ngủ; bộ mặc nhà hoặc bộ T-shirt+short không tự động là pyjama. Không áp cho mọi đồ ngủ liền thân.","gate_profile":"SLEEPWEAR","local_semantic_auto_policy":"REVIEW_REQUIRED","source_id":"S02","pdf_page_1_based":11},
    {"rule_id":"T065","family":"SLEEPWEAR","tnved10":"6108310000","competition_group":"APPAREL_TEXTILE","tariff_gender":"F","height_scope":"NON_BABY","construction_vi_ru":"Dệt kim / трикотаж","product_vi":"Bộ pyjama, áo/váy ngủ","material_condition_vi":"Cotton / хлопок — sau khi áp dụng quy tắc hỗn hợp","conditions_vi":"Phải nhận biết thực sự là đồ ngủ; bộ mặc nhà hoặc bộ T-shirt+short không tự động là pyjama. Không áp cho mọi đồ ngủ liền thân.","gate_profile":"SLEEPWEAR","local_semantic_auto_policy":"REVIEW_REQUIRED","source_id":"S02","pdf_page_1_based":11},
    {"rule_id":"T066","family":"SLEEPWEAR","tnved10":"6108320000","competition_group":"APPAREL_TEXTILE","tariff_gender":"F","height_scope":"NON_BABY","construction_vi_ru":"Dệt kim / трикотаж","product_vi":"Bộ pyjama, áo/váy ngủ","material_condition_vi":"Sợi hóa học / химические: tổng hợp hoặc nhân tạo","conditions_vi":"Phải nhận biết thực sự là đồ ngủ; bộ mặc nhà hoặc bộ T-shirt+short không tự động là pyjama. Không áp cho mọi đồ ngủ liền thân.","gate_profile":"SLEEPWEAR","local_semantic_auto_policy":"REVIEW_REQUIRED","source_id":"S02","pdf_page_1_based":11},
    {"rule_id":"T067","family":"SLEEPWEAR","tnved10":"6207210000","competition_group":"APPAREL_TEXTILE","tariff_gender":"M","height_scope":"NON_BABY","construction_vi_ru":"Dệt thoi / нетрикотаж","product_vi":"Bộ pyjama, áo/váy ngủ","material_condition_vi":"Cotton / хлопок — sau khi áp dụng quy tắc hỗn hợp","conditions_vi":"Phải nhận biết thực sự là đồ ngủ; bộ mặc nhà hoặc bộ T-shirt+short không tự động là pyjama. Không áp cho mọi đồ ngủ liền thân.","gate_profile":"SLEEPWEAR","local_semantic_auto_policy":"REVIEW_REQUIRED","source_id":"S03","pdf_page_1_based":13},
    {"rule_id":"T068","family":"SLEEPWEAR","tnved10":"6207220000","competition_group":"APPAREL_TEXTILE","tariff_gender":"M","height_scope":"NON_BABY","construction_vi_ru":"Dệt thoi / нетрикотаж","product_vi":"Bộ pyjama, áo/váy ngủ","material_condition_vi":"Sợi hóa học / химические: tổng hợp hoặc nhân tạo","conditions_vi":"Phải nhận biết thực sự là đồ ngủ; bộ mặc nhà hoặc bộ T-shirt+short không tự động là pyjama. Không áp cho mọi đồ ngủ liền thân.","gate_profile":"SLEEPWEAR","local_semantic_auto_policy":"REVIEW_REQUIRED","source_id":"S03","pdf_page_1_based":13},
    {"rule_id":"T069","family":"SLEEPWEAR","tnved10":"6208210000","competition_group":"APPAREL_TEXTILE","tariff_gender":"F","height_scope":"NON_BABY","construction_vi_ru":"Dệt thoi / нетрикотаж","product_vi":"Bộ pyjama, áo/váy ngủ","material_condition_vi":"Cotton / хлопок — sau khi áp dụng quy tắc hỗn hợp","conditions_vi":"Phải nhận biết thực sự là đồ ngủ; bộ mặc nhà hoặc bộ T-shirt+short không tự động là pyjama. Không áp cho mọi đồ ngủ liền thân.","gate_profile":"SLEEPWEAR","local_semantic_auto_policy":"REVIEW_REQUIRED","source_id":"S03","pdf_page_1_based":14},
    {"rule_id":"T070","family":"SLEEPWEAR","tnved10":"6208220000","competition_group":"APPAREL_TEXTILE","tariff_gender":"F","height_scope":"NON_BABY","construction_vi_ru":"Dệt thoi / нетрикотаж","product_vi":"Bộ pyjama, áo/váy ngủ","material_condition_vi":"Sợi hóa học / химические: tổng hợp hoặc nhân tạo","conditions_vi":"Phải nhận biết thực sự là đồ ngủ; bộ mặc nhà hoặc bộ T-shirt+short không tự động là pyjama. Không áp cho mọi đồ ngủ liền thân.","gate_profile":"SLEEPWEAR","local_semantic_auto_policy":"REVIEW_REQUIRED","source_id":"S03","pdf_page_1_based":14},
    {"rule_id":"T071","family":"KNIT_UNDERPANTS","tnved10":"6107110000","competition_group":"APPAREL_TEXTILE","tariff_gender":"M","height_scope":"NON_BABY","construction_vi_ru":"Dệt kim / трикотаж","product_vi":"Quần lót, quần lót dài","material_condition_vi":"Cotton / хлопок — sau khi áp dụng quy tắc hỗn hợp","conditions_vi":"Quần lót; áo ba lỗ thông thường xét 6109; bra xét 6212.","gate_profile":"KNIT_UNDERPANTS","local_semantic_auto_policy":"ELIGIBLE_IF_ALL_GATES","source_id":"S02","pdf_page_1_based":10},
    {"rule_id":"T072","family":"KNIT_UNDERPANTS","tnved10":"6107120000","competition_group":"APPAREL_TEXTILE","tariff_gender":"M","height_scope":"NON_BABY","construction_vi_ru":"Dệt kim / трикотаж","product_vi":"Quần lót, quần lót dài","material_condition_vi":"Sợi hóa học / химические: tổng hợp hoặc nhân tạo","conditions_vi":"Quần lót; áo ba lỗ thông thường xét 6109; bra xét 6212.","gate_profile":"KNIT_UNDERPANTS","local_semantic_auto_policy":"ELIGIBLE_IF_ALL_GATES","source_id":"S02","pdf_page_1_based":10},
    {"rule_id":"T073","family":"KNIT_UNDERPANTS","tnved10":"6108210000","competition_group":"APPAREL_TEXTILE","tariff_gender":"F","height_scope":"NON_BABY","construction_vi_ru":"Dệt kim / трикотаж","product_vi":"Quần lót, quần lót dài","material_condition_vi":"Cotton / хлопок — sau khi áp dụng quy tắc hỗn hợp","conditions_vi":"Quần lót; áo ba lỗ thông thường xét 6109; bra xét 6212.","gate_profile":"KNIT_UNDERPANTS","local_semantic_auto_policy":"ELIGIBLE_IF_ALL_GATES","source_id":"S02","pdf_page_1_based":11},
    {"rule_id":"T074","family":"KNIT_UNDERPANTS","tnved10":"6108220000","competition_group":"APPAREL_TEXTILE","tariff_gender":"F","height_scope":"NON_BABY","construction_vi_ru":"Dệt kim / трикотаж","product_vi":"Quần lót, quần lót dài","material_condition_vi":"Sợi hóa học / химические: tổng hợp hoặc nhân tạo","conditions_vi":"Quần lót; áo ba lỗ thông thường xét 6109; bra xét 6212.","gate_profile":"KNIT_UNDERPANTS","local_semantic_auto_policy":"ELIGIBLE_IF_ALL_GATES","source_id":"S02","pdf_page_1_based":11},
    {"rule_id":"T075","family":"BRA_SINGLE","tnved10":"6212109000","competition_group":"APPAREL_TEXTILE","tariff_gender":"ANY","height_scope":"NOT_APPLICABLE","construction_vi_ru":"Dệt kim hoặc không dệt kim","product_vi":"Áo ngực bán riêng","material_condition_vi":"Mã này không tách theo sợi","conditions_vi":"Áo ngực thông thường; không bộ bán lẻ gồm bra+quần lót.","gate_profile":"BRA_SINGLE","local_semantic_auto_policy":"REVIEW_REQUIRED","source_id":"S03","pdf_page_1_based":17},
    {"rule_id":"T076","family":"BRA_BRIEF_SET","tnved10":"6212101000","competition_group":"APPAREL_TEXTILE","tariff_gender":"ANY","height_scope":"NOT_APPLICABLE","construction_vi_ru":"Dệt kim hoặc không dệt kim","product_vi":"Bộ áo ngực kèm quần lót","material_condition_vi":"Mã này không tách theo sợi","conditions_vi":"Bộ bán lẻ thực sự gồm áo ngực và quần lót; không phải mọi bộ đồ lót.","gate_profile":"BRA_BRIEF_SET","local_semantic_auto_policy":"REVIEW_REQUIRED","source_id":"S03","pdf_page_1_based":17},
    {"rule_id":"T077","family":"WOVEN_OUTER_JACKET","tnved10":"6201300000","competition_group":"APPAREL_TEXTILE","tariff_gender":"M","height_scope":"NON_BABY","construction_vi_ru":"Dệt thoi / нетрикотаж","product_vi":"Áo khoác gió, áo jacket có/không lớp nhồi","material_condition_vi":"Cotton / хлопок — sau khi áp dụng quy tắc hỗn hợp","conditions_vi":"Áo jacket/vetrovka ngoài dệt thoi; không blazer, không vải thuộc 6210, không bộ trượt tuyết. Xác định sợi ở phần quyết định phân loại; lớp nhồi пух/холлофайбер không tự quyết định mã.","gate_profile":"WOVEN_OUTER_JACKET","local_semantic_auto_policy":"REVIEW_REQUIRED","source_id":"S03","pdf_page_1_based":4},
    {"rule_id":"T078","family":"WOVEN_OUTER_JACKET","tnved10":"6201400000","competition_group":"APPAREL_TEXTILE","tariff_gender":"M","height_scope":"NON_BABY","construction_vi_ru":"Dệt thoi / нетрикотаж","product_vi":"Áo khoác gió, áo jacket có/không lớp nhồi","material_condition_vi":"Sợi hóa học / химические: tổng hợp hoặc nhân tạo","conditions_vi":"Áo jacket/vetrovka ngoài dệt thoi; không blazer, không vải thuộc 6210, không bộ trượt tuyết. Xác định sợi ở phần quyết định phân loại; lớp nhồi пух/холлофайбер không tự quyết định mã.","gate_profile":"WOVEN_OUTER_JACKET","local_semantic_auto_policy":"REVIEW_REQUIRED","source_id":"S03","pdf_page_1_based":5},
    {"rule_id":"T079","family":"WOVEN_OUTER_JACKET","tnved10":"6202300000","competition_group":"APPAREL_TEXTILE","tariff_gender":"F","height_scope":"NON_BABY","construction_vi_ru":"Dệt thoi / нетрикотаж","product_vi":"Áo khoác gió, áo jacket có/không lớp nhồi","material_condition_vi":"Cotton / хлопок — sau khi áp dụng quy tắc hỗn hợp","conditions_vi":"Áo jacket/vetrovka ngoài dệt thoi; không blazer, không vải thuộc 6210, không bộ trượt tuyết. Xác định sợi ở phần quyết định phân loại; lớp nhồi пух/холлофайбер không tự quyết định mã.","gate_profile":"WOVEN_OUTER_JACKET","local_semantic_auto_policy":"REVIEW_REQUIRED","source_id":"S03","pdf_page_1_based":5},
    {"rule_id":"T080","family":"WOVEN_OUTER_JACKET","tnved10":"6202400009","competition_group":"APPAREL_TEXTILE","tariff_gender":"F","height_scope":"NON_BABY","construction_vi_ru":"Dệt thoi / нетрикотаж","product_vi":"Áo khoác gió, áo jacket có/không lớp nhồi","material_condition_vi":"Sợi hóa học / химические: tổng hợp hoặc nhân tạo","conditions_vi":"Áo jacket/vetrovka ngoài dệt thoi; không blazer, không vải thuộc 6210, không bộ trượt tuyết. Xác định sợi ở phần quyết định phân loại; lớp nhồi пух/холлофайбер không tự quyết định mã. Mã …0001 chỉ пальто/полупальто/накидки/плащи tương tự >1kg; jacket không tự chuyển vì >1kg.","gate_profile":"WOVEN_OUTER_JACKET","local_semantic_auto_policy":"REVIEW_REQUIRED","source_id":"S03","pdf_page_1_based":5},
    {"rule_id":"T081","family":"KNIT_TRACKSUIT","tnved10":"6112110000","competition_group":"APPAREL_TEXTILE","tariff_gender":"ANY","height_scope":"NON_BABY","construction_vi_ru":"Dệt kim / трикотаж","product_vi":"Bộ thể thao dệt kim thực sự","material_condition_vi":"Cotton / хлопок — sau khi áp dụng quy tắc hỗn hợp","conditions_vi":"Phải thỏa kiểu bộ thể thao theo chú giải 6112; không tự áp cho T-shirt+short/quần lẻ.","gate_profile":"KNIT_TRACKSUIT","local_semantic_auto_policy":"REVIEW_REQUIRED","source_id":"S02","pdf_page_1_based":14},
    {"rule_id":"T082","family":"KNIT_TRACKSUIT","tnved10":"6112120000","competition_group":"APPAREL_TEXTILE","tariff_gender":"ANY","height_scope":"NON_BABY","construction_vi_ru":"Dệt kim / трикотаж","product_vi":"Bộ thể thao dệt kim thực sự","material_condition_vi":"Sợi tổng hợp / синтетические: polyester, polyamide, elastane…","conditions_vi":"Phải thỏa kiểu bộ thể thao theo chú giải 6112; không tự áp cho T-shirt+short/quần lẻ.","gate_profile":"KNIT_TRACKSUIT","local_semantic_auto_policy":"REVIEW_REQUIRED","source_id":"S02","pdf_page_1_based":14},
    {"rule_id":"T083","family":"WOVEN_TRACKSUIT_LINED","tnved10":"6211333100","competition_group":"APPAREL_TEXTILE","tariff_gender":"M","height_scope":"NON_BABY","construction_vi_ru":"Dệt thoi / нетрикотаж","product_vi":"Bộ thể thao dệt thoi có lót","material_condition_vi":"Sợi hóa học / химические: tổng hợp hoặc nhân tạo","conditions_vi":"Bộ thể thao có lót; mặt ngoài các phần từ cùng một loại vật liệu. Loại không lót, khác vật liệu mặt hoặc phần trên/dưới rời phải xét nhánh khác.","gate_profile":"WOVEN_TRACKSUIT_LINED","local_semantic_auto_policy":"REVIEW_REQUIRED","source_id":"S03","pdf_page_1_based":16},
    {"rule_id":"T084","family":"WOVEN_TRACKSUIT_LINED","tnved10":"6211433100","competition_group":"APPAREL_TEXTILE","tariff_gender":"F","height_scope":"NON_BABY","construction_vi_ru":"Dệt thoi / нетрикотаж","product_vi":"Bộ thể thao dệt thoi có lót","material_condition_vi":"Sợi hóa học / химические: tổng hợp hoặc nhân tạo","conditions_vi":"Bộ thể thao có lót; mặt ngoài các phần từ cùng một loại vật liệu. Loại không lót, khác vật liệu mặt hoặc phần trên/dưới rời phải xét nhánh khác.","gate_profile":"WOVEN_TRACKSUIT_LINED","local_semantic_auto_policy":"REVIEW_REQUIRED","source_id":"S03","pdf_page_1_based":17},
    {"rule_id":"T085","family":"BABY_APPAREL","tnved10":"6111209000","competition_group":"APPAREL_TEXTILE","tariff_gender":"ANY","height_scope":"BABY_LE_86","construction_vi_ru":"Dệt kim / трикотаж","product_vi":"Quần áo em bé chiều cao tối đa 86 cm","material_condition_vi":"Cotton / хлопок — sau khi áp dụng quy tắc hỗn hợp","conditions_vi":"Ưu tiên nhóm em bé cho loại quần áo thuộc chương tương ứng; dệt kim mã này loại trừ găng tay. Không kéo mã em bé sang size >86cm.","gate_profile":"BABY_APPAREL","local_semantic_auto_policy":"ELIGIBLE_IF_ALL_GATES","source_id":"S02","pdf_page_1_based":14},
    {"rule_id":"T086","family":"BABY_APPAREL","tnved10":"6111309000","competition_group":"APPAREL_TEXTILE","tariff_gender":"ANY","height_scope":"BABY_LE_86","construction_vi_ru":"Dệt kim / трикотаж","product_vi":"Quần áo em bé chiều cao tối đa 86 cm","material_condition_vi":"Sợi tổng hợp / синтетические: polyester, polyamide, elastane…","conditions_vi":"Ưu tiên nhóm em bé cho loại quần áo thuộc chương tương ứng; dệt kim mã này loại trừ găng tay. Không kéo mã em bé sang size >86cm.","gate_profile":"BABY_APPAREL","local_semantic_auto_policy":"ELIGIBLE_IF_ALL_GATES","source_id":"S02","pdf_page_1_based":14},
    {"rule_id":"T087","family":"BABY_APPAREL","tnved10":"6209200000","competition_group":"APPAREL_TEXTILE","tariff_gender":"ANY","height_scope":"BABY_LE_86","construction_vi_ru":"Dệt thoi / нетрикотаж","product_vi":"Quần áo em bé chiều cao tối đa 86 cm","material_condition_vi":"Cotton / хлопок — sau khi áp dụng quy tắc hỗn hợp","conditions_vi":"Ưu tiên nhóm em bé cho loại quần áo thuộc chương tương ứng; dệt kim mã này loại trừ găng tay. Không kéo mã em bé sang size >86cm.","gate_profile":"BABY_APPAREL","local_semantic_auto_policy":"ELIGIBLE_IF_ALL_GATES","source_id":"S03","pdf_page_1_based":14},
    {"rule_id":"T088","family":"BABY_APPAREL","tnved10":"6209300000","competition_group":"APPAREL_TEXTILE","tariff_gender":"ANY","height_scope":"BABY_LE_86","construction_vi_ru":"Dệt thoi / нетрикотаж","product_vi":"Quần áo em bé chiều cao tối đa 86 cm","material_condition_vi":"Sợi tổng hợp / синтетические: polyester, polyamide, elastane…","conditions_vi":"Ưu tiên nhóm em bé cho loại quần áo thuộc chương tương ứng; dệt kim mã này loại trừ găng tay. Không kéo mã em bé sang size >86cm.","gate_profile":"BABY_APPAREL","local_semantic_auto_policy":"ELIGIBLE_IF_ALL_GATES","source_id":"S03","pdf_page_1_based":14},
    {"rule_id":"T089","family":"SKI_SUIT","tnved10":"6112200000","competition_group":"APPAREL_TEXTILE","tariff_gender":"ANY","height_scope":"NON_BABY","construction_vi_ru":"Dệt kim / трикотаж","product_vi":"Bộ hoặc áo quần liền thân trượt tuyết","material_condition_vi":"Mã này không tách theo sợi","conditions_vi":"Phải thỏa chú giải7 của chương: đồ liền thân hoặc bộ áo+quần thực sự nhận diện để trượt tuyết; quần ấm/quần tuyết bán riêng không tự là лыжный костюм.","gate_profile":"SKI_SUIT","local_semantic_auto_policy":"REVIEW_REQUIRED","source_id":"S02","pdf_page_1_based":14},
    {"rule_id":"T090","family":"SKI_SUIT","tnved10":"6211200000","competition_group":"APPAREL_TEXTILE","tariff_gender":"ANY","height_scope":"NON_BABY","construction_vi_ru":"Dệt thoi / нетрикотаж","product_vi":"Bộ hoặc áo quần liền thân trượt tuyết","material_condition_vi":"Mã này không tách theo sợi","conditions_vi":"Phải thỏa chú giải7 của chương: đồ liền thân hoặc bộ áo+quần thực sự nhận diện để trượt tuyết; quần ấm/quần tuyết bán riêng không tự là лыжный костюм.","gate_profile":"SKI_SUIT","local_semantic_auto_policy":"REVIEW_REQUIRED","source_id":"S03","pdf_page_1_based":15},
    {"rule_id":"T091","family":"SCARF","tnved10":"6117100000","competition_group":"APPAREL_TEXTILE","tariff_gender":"ANY","height_scope":"NON_BABY","construction_vi_ru":"Dệt kim / трикотаж","product_vi":"Khăn quàng, khăn choàng","material_condition_vi":"Không tách theo sợi ở mã này","conditions_vi":"Khăn quàng/khăn choàng thông thường; không cà vạt, mũ hoặc khăn tay.","gate_profile":"SCARF","local_semantic_auto_policy":"REVIEW_REQUIRED","source_id":"S02","pdf_page_1_based":18},
    {"rule_id":"T092","family":"SCARF","tnved10":"6214100000","competition_group":"APPAREL_TEXTILE","tariff_gender":"ANY","height_scope":"NON_BABY","construction_vi_ru":"Dệt thoi / нетрикотаж","product_vi":"Khăn quàng, khăn choàng","material_condition_vi":"Tơ tằm / шёлк","conditions_vi":"Khăn quàng/khăn choàng thông thường; không cà vạt, mũ hoặc khăn tay. Khăn vuông/gần vuông có tất cả các cạnh ≤60cm thuộc6213 theo ghi chú8 chương62.","gate_profile":"SCARF","local_semantic_auto_policy":"REVIEW_REQUIRED","source_id":"S03","pdf_page_1_based":18},
    {"rule_id":"T093","family":"SCARF","tnved10":"6214200000","competition_group":"APPAREL_TEXTILE","tariff_gender":"ANY","height_scope":"NON_BABY","construction_vi_ru":"Dệt thoi / нетрикотаж","product_vi":"Khăn quàng, khăn choàng","material_condition_vi":"Len/lông động vật mịn / шерсть, тонкий волос","conditions_vi":"Khăn quàng/khăn choàng thông thường; không cà vạt, mũ hoặc khăn tay. Khăn vuông/gần vuông có tất cả các cạnh ≤60cm thuộc6213 theo ghi chú8 chương62.","gate_profile":"SCARF","local_semantic_auto_policy":"REVIEW_REQUIRED","source_id":"S03","pdf_page_1_based":18},
    {"rule_id":"T094","family":"SCARF","tnved10":"6214300000","competition_group":"APPAREL_TEXTILE","tariff_gender":"ANY","height_scope":"NON_BABY","construction_vi_ru":"Dệt thoi / нетрикотаж","product_vi":"Khăn quàng, khăn choàng","material_condition_vi":"Tổng hợp / синтетические","conditions_vi":"Khăn quàng/khăn choàng thông thường; không cà vạt, mũ hoặc khăn tay. Khăn vuông/gần vuông có tất cả các cạnh ≤60cm thuộc6213 theo ghi chú8 chương62.","gate_profile":"SCARF","local_semantic_auto_policy":"REVIEW_REQUIRED","source_id":"S03","pdf_page_1_based":18},
    {"rule_id":"T095","family":"SCARF","tnved10":"6214400000","competition_group":"APPAREL_TEXTILE","tariff_gender":"ANY","height_scope":"NON_BABY","construction_vi_ru":"Dệt thoi / нетрикотаж","product_vi":"Khăn quàng, khăn choàng","material_condition_vi":"Nhân tạo / искусственные","conditions_vi":"Khăn quàng/khăn choàng thông thường; không cà vạt, mũ hoặc khăn tay. Khăn vuông/gần vuông có tất cả các cạnh ≤60cm thuộc6213 theo ghi chú8 chương62.","gate_profile":"SCARF","local_semantic_auto_policy":"REVIEW_REQUIRED","source_id":"S03","pdf_page_1_based":18},
    {"rule_id":"T096","family":"SCARF","tnved10":"6214900000","competition_group":"APPAREL_TEXTILE","tariff_gender":"ANY","height_scope":"NON_BABY","construction_vi_ru":"Dệt thoi / нетрикотаж","product_vi":"Khăn quàng, khăn choàng","material_condition_vi":"Khác; cotton hoặc lanh / хлопок, лён","conditions_vi":"Khăn quàng/khăn choàng thông thường; không cà vạt, mũ hoặc khăn tay. Khăn vuông/gần vuông có tất cả các cạnh ≤60cm thuộc6213 theo ghi chú8 chương62.","gate_profile":"SCARF","local_semantic_auto_policy":"REVIEW_REQUIRED","source_id":"S03","pdf_page_1_based":18},
    {"rule_id":"T097","family":"BEANIE","tnved10":"6505009000","competition_group":"HEADGEAR","tariff_gender":"ANY","height_scope":"NOT_APPLICABLE","construction_vi_ru":"Dệt kim / трикотаж","product_vi":"Mũ len, beanie dệt kim thường","material_condition_vi":"Dệt kim từ sợi dệt; mã này không tách cotton/tổng hợp/len","conditions_vi":"Không mũ bảo hộ, không mũ lông thú, không mũ có lưỡi trai. Kiểm phạm vi маркировка theo tên hàng và mã.","gate_profile":"BEANIE","local_semantic_auto_policy":"ELIGIBLE_IF_ALL_GATES","source_id":"S08","pdf_page_1_based":2},
    {"rule_id":"T098","family":"TEXTILE_VISOR_CAP","tnved10":"6505003000","competition_group":"HEADGEAR","tariff_gender":"ANY","height_scope":"NOT_APPLICABLE","construction_vi_ru":"Vật liệu dệt / текстиль","product_vi":"Mũ lưỡi trai bằng vật liệu dệt","material_condition_vi":"Mặt hàng thuộc 6505; không nhựa/cao su/lông thú","conditions_vi":"Phải là mũ có lưỡi trai. Không tự dùng cho beanie; không mũ bảo hộ.","gate_profile":"TEXTILE_VISOR_CAP","local_semantic_auto_policy":"ELIGIBLE_IF_ALL_GATES","source_id":"S08","pdf_page_1_based":2},
    {"rule_id":"T099","family":"HANDBAG","tnved10":"4202210000","competition_group":"BAG_CONTAINER","tariff_gender":"ANY","height_scope":"NOT_APPLICABLE","construction_vi_ru":"Mặt ngoài / лицевая поверхность","product_vi":"Túi xách / đeo vai mặt da","material_condition_vi":"Da thật hoặc da tái tạo / натуральная или композиционная кожа","conditions_vi":"Kiểu handbag; có/không quai. Không ba lô, túi du lịch, cặp hồ sơ. Mã phân theo bề mặt nhìn thấy. Chưa xác nhận nghĩa vụ маркировка cho 4202 trong nghiên cứu này.","gate_profile":"HANDBAG","local_semantic_auto_policy":"REVIEW_REQUIRED","source_id":"S09","pdf_page_1_based":4},
    {"rule_id":"T100","family":"HANDBAG","tnved10":"4202221000","competition_group":"BAG_CONTAINER","tariff_gender":"ANY","height_scope":"NOT_APPLICABLE","construction_vi_ru":"Mặt ngoài / лицевая поверхность","product_vi":"Túi xách mặt nhựa, PU/PVC khi phù hợp","material_condition_vi":"Bề mặt nhựa tấm / листы пластмассы","conditions_vi":"Kiểu handbag; phải xác nhận bề mặt nhìn thấy là nhựa tấm. Tên экокожа không đủ chốt mã. Chưa xác nhận nghĩa vụ маркировка cho 4202.","gate_profile":"HANDBAG","local_semantic_auto_policy":"REVIEW_REQUIRED","source_id":"S09","pdf_page_1_based":4},
    {"rule_id":"T101","family":"HANDBAG","tnved10":"4202229000","competition_group":"BAG_CONTAINER","tariff_gender":"ANY","height_scope":"NOT_APPLICABLE","construction_vi_ru":"Mặt ngoài / лицевая поверхность","product_vi":"Túi xách mặt vải","material_condition_vi":"Vật liệu dệt / текстильные материалы","conditions_vi":"Kiểu handbag, không túi du lịch/ba lô. Phân theo mặt nhìn thấy; không theo lớp lót. Chưa xác nhận nghĩa vụ маркировка cho 4202.","gate_profile":"HANDBAG","local_semantic_auto_policy":"REVIEW_REQUIRED","source_id":"S09","pdf_page_1_based":4},
    {"rule_id":"T102","family":"BACKPACK_TRAVEL_SPORT_BAG","tnved10":"4202911000","competition_group":"BAG_CONTAINER","tariff_gender":"ANY","height_scope":"NOT_APPLICABLE","construction_vi_ru":"Mặt ngoài / лицевая поверхность","product_vi":"Ba lô, túi du lịch/thể thao mặt da","material_condition_vi":"Da thật hoặc da tái tạo","conditions_vi":"Phải đúng loại ba lô/túi du lịch/thể thao; không handbag/cặp hồ sơ hoặc cặp học sinh cấu tạo khác. Chưa xác nhận nghĩa vụ маркировка cho 4202.","gate_profile":"BACKPACK_TRAVEL_SPORT_BAG","local_semantic_auto_policy":"REVIEW_REQUIRED","source_id":"S09","pdf_page_1_based":5},
    {"rule_id":"T103","family":"BACKPACK_TRAVEL_SPORT_BAG","tnved10":"4202921100","competition_group":"BAG_CONTAINER","tariff_gender":"ANY","height_scope":"NOT_APPLICABLE","construction_vi_ru":"Mặt ngoài / лицевая поверхность","product_vi":"Ba lô, túi du lịch/thể thao mặt nhựa","material_condition_vi":"Bề mặt nhựa tấm / листы пластмассы","conditions_vi":"Xác nhận loại túi và bề mặt nhìn thấy; không gán chỉ theo tên PU hoặc экокожа. Chưa xác nhận nghĩa vụ маркировка cho 4202.","gate_profile":"BACKPACK_TRAVEL_SPORT_BAG","local_semantic_auto_policy":"REVIEW_REQUIRED","source_id":"S09","pdf_page_1_based":5},
    {"rule_id":"T104","family":"BACKPACK_TRAVEL_SPORT_BAG","tnved10":"4202929100","competition_group":"BAG_CONTAINER","tariff_gender":"ANY","height_scope":"NOT_APPLICABLE","construction_vi_ru":"Mặt ngoài / лицевая поверхность","product_vi":"Ba lô, túi du lịch/thể thao mặt vải","material_condition_vi":"Vật liệu dệt / текстильные материалы","conditions_vi":"Phải đúng loại ba lô/túi du lịch/thể thao; không handbag/cặp hồ sơ hoặc cặp học sinh cấu tạo khác. Chưa xác nhận nghĩa vụ маркировка cho 4202.","gate_profile":"BACKPACK_TRAVEL_SPORT_BAG","local_semantic_auto_policy":"REVIEW_REQUIRED","source_id":"S09","pdf_page_1_based":5}
  ]
}
```

## 17 Thứ tự xây dựng và tiêu chí bàn giao

### 17 1 Các bước triển khai

1. Đọc codebase hiện tại, xác định nguồn danh sách sản phẩm và nơi lưu TNVED/gender. Lập mapping cột/field có kiểu dữ liệu và ví dụ thật đã ẩn dữ liệu nhạy cảm.
2. Tạo importer bảo toàn raw, normalizer và lớp provenance; dựng báo cáo thiếu dữ liệu trước khi làm chức năng sửa.
3. Trích seed104 và tạo bộ compiler/rule xác định. Tách material resolver và gender resolver; viết các ca ranh giới trước khi bật rule.
4. Tích hợp danh mục EEC có phiên bản. Nếu chưa có nguồn đầy đủ, báo rõ phạm vi mã kiểm được và giữ UNKNOWN cho mã chưa xác nhận.
5. Thêm adapter chỉ đọc WB/NC, từ điển đúng subject và cổng scope theo thẻ. Không hardcode ID của một shop sang shop khác.
6. Tạo patch plan, bộ lọc điều kiện tự áp dụng nội bộ và audit log.
7. Thử nghiệm connector bằng dữ liệu/đối tượng thử nghiệm đã được cho phép. Xác minh ngữ nghĩa cập nhật, bảo toàn field và kết quả đọc lại.
8. Chỉ bật đồng bộ cho adapter đã xác minh, đúng tài khoản và chính sách. Luồng NC bắt buộc đi theo khả năng hiện hành; trường hợp hỗ trợ xử lý giữ trạng thái chờ.

### 17 2 Tiêu chí hoàn thành

- Danh sách sản phẩm nhập được và mỗi biến thể có khóa cùng nguồn rõ ràng.
- Mỗi kết luận có trạng thái theo từng dimension, rule_id, mã ứng viên, bằng chứng và trường thiếu.
- Bộ mã seed104 không bị thiếu, trùng ID hoặc biến thành số gây mất chữ số.
- Các ca nghiệm thu đã được chuyển thành test độc lập và kết quả thực tế được ghi nhận.
- Không có đường đi nào tự sửa nội dung từ dữ liệu UNKNOWN/INFERRED, nguồn mâu thuẫn hoặc rule chưa compile.
- Thay đổi giới tính thương mại không được kích hoạt bằng mã TNVED.
- Thay đổi TNVED không tự tạo/sửa GTIN, barcode, thành phần, size hoặc màu.
- Connector chưa xác minh không được ghi thật; connector có ghi phải bảo toàn dữ liệu và đọc lại xác nhận.
- Báo cáo phân biệt “đã đề xuất”, “đã sửa nội bộ”, “chờ NC”, “đã xác nhận WB” và “đồng bộ một phần”.
- Có tài liệu vận hành cho bổ sung rule, cập nhật nguồn, xử lý thiếu dữ liệu và thay đổi schema.

### 17 3 Lệnh giao việc có thể dán vào Antigravity

```text
Hãy đọc toàn bộ tài liệu Antigravity_TNVED_Gender_Validator_Spec_vi.md
và triển khai bộ kiểm định trong dự án hiện tại.

Trước hết kiểm tra cấu trúc codebase và nguồn dữ liệu sản phẩm.
Giữ stack hiện có. Triển khai importer, provenance, validator xác định,
material resolver, tariff gender resolver, báo cáo và patch plan.
Trích đủ 104 dòng từ marker MATRIX_TNVED_104_V1; không coi seed prose
là rule executable. Biên dịch các điều kiện thành predicate ba giá trị
và kiểm thử các trường hợp ở phần 14 trước khi bật rule.

Tách commercial_gender khỏi tariff_gender. Không suy thuộc tính từ GTIN.
UNKNOWN, NOT_COVERED và CONFLICT không được tự sửa.
Nếu thiếu dữ liệu, báo đúng tên trường cần bổ sung.
Cho phép tự sửa nội bộ khi đủ chính sách và cổng ở phần 10.
Chỉ ghi WB/NC khi adapter, field scope, quyền và cách kiểm tra đọc lại
đã được xác minh; giữ đầy đủ dữ liệu ngoài patch.
Luồng sửa thuộc tính bắt buộc NC phải theo khả năng thật, không giả định API.

Hoàn thành theo từng module có test. Báo rõ phần đã triển khai,
phần đang mock/chỉ đọc, kết quả test thực tế và đầu vào còn cần.
Không tuyên bố sản phẩm tuân thủ toàn bộ pháp luật chỉ vì TNVED khớp.
```

## 18 Nguồn chính thức và phạm vi sử dụng

Nguồn kiểm tra ngày 07.10.2026. Ngày xuất bản/cập nhật khi có được ghi riêng. Phải xem thay đổi và ngày hiệu lực khi áp dụng cho thời điểm khác. URL là điểm tra cứu chính thức; tên enum và chính sách phần mềm trong đặc tả không được gán là quy định của các tổ chức này.

| ID | Nguồn | Phạm vi |
|---|---|---|
| S01 | [EEC — Danh mục TNVED và ETT hiện hành](https://eec.eaeunion.org/comission/department/catr/ett/) | Danh mục gốc và các quyết định sửa đổi; dùng để xác định phiên bản có hiệu lực. |
| S02 | [EEC — Chương 61](https://eec.eaeunion.org/upload/files/catr/ett/ru.61_2022_25.04.2022.pdf) | Mã dệt kim; chú giải về kiểu áo, trẻ ≤86 cm, giới tính, thứ tự ưu tiên. |
| S03 | [EEC — Chương 62](https://eec.eaeunion.org/upload/files/catr/ett/ru.62_2022.pdf) | Mã quần áo không dệt kim trong phạm vi tài liệu; chú giải 6209, 6210, 6212, khăn và giới tính. |
| S04 | [EEC — Chú giải Phần XI](https://eec.eaeunion.org/upload/files/catr/ett/ru.50_2022.pdf) | Quy tắc sợi pha và phần vật liệu quyết định phân loại; không dùng MAX từng tên sợi. |
| S05 | [EEC — Giải thích Phần XI](https://eec.eaeunion.org/upload/files/catr/psn/psn50.pdf) | Ví dụ gộp vật liệu theo nhóm và phân tiếp trong nhóm; trang PDF 8–9. |
| S06 | [EEC — Định nghĩa sợi hóa học](https://eec.eaeunion.org/comission/department/catr/ett/ru.2022/ru.54_2022_08.03.2026.pdf) | Phân biệt synthetic và artificial. |
| S07 | [EEC — Giải thích Chương 61](https://eec.eaeunion.org/upload/files/catr/psn/tom_vi/%D0%B3%D1%80%2061.pdf) | Phân biệt áo dệt kim thông thường và nhánh áo nhẹ mỏng cổ kín. |
| S08 | [EEC — Chương 65](https://eec.eaeunion.org/upload/files/catr/ett/ru.65_2022.pdf) | Mũ beanie và mũ có lưỡi trai. |
| S09 | [EEC — Chương 42](https://eec.eaeunion.org/upload/files/catr/ett/ru.42_2022.pdf) | Túi theo loại và bề mặt ngoài nhìn thấy. |
| S10 | [WB — Маркировка товаров и поставок](https://seller.wildberries.ru/instructions/ru/ru/material/items-and-shipment-labling-like-barcode-and-others) | Cập nhật 02.10.2026; kiểm GTIN từ 01.10.2026 trong phạm vi nêu trong hướng dẫn. |
| S11 | [WB — API làm việc với sản phẩm](https://dev.wildberries.ru/en/openapi/work-with-products) | Từ điển TNVED theo subjectID, đặc tính theo subject, đọc và cập nhật thẻ. |
| S12 | [WB — Hướng dẫn API sản phẩm](https://dev.wildberries.ru/en/news/101) | Ngày 09.12.2025; cập nhật thẻ, bảo toàn dữ liệu và kiểm tra kết quả. Đối chiếu lại schema khi triển khai. |
| S13 | [WB — Hướng dẫn tạo thẻ phiên bản Belarus](https://seller.wildberries.ru/instructions/ru/by/material/how-to-create-card) | Cập nhật 17.09.2026; chuyển Код ТН ВЭД cũ sang field ТН ВЭД riêng. Kiểm tra binding đúng locale/tài khoản. |
| S14 | [Честный ЗНАК — Mỗi màu và size có thẻ riêng](https://markirovka.ru/knowledge/tovarnye-gruppy/legkaya-promishlennost/skolko-dolzhno-byt-kartochek-tovarov-dlya-neskolkikh-tsvetov-i-razmerov) | Ngày 13.05.2025; mô tả đầy đủ: một GTIN tương ứng một thẻ và biến thể hàng hóa. |
| S15 | [GS1 Russia — GTIN không mã hóa thuộc tính](https://www.gs1ru.org/faq/q-hidden-information/) | Không suy TNVED, giới tính, chất liệu từ chữ số GTIN. |
| S16 | [Честный ЗНАК — Cấu trúc mã маркировка](https://markirovka.ru/knowledge/tovarnye-gruppy/legkaya-promishlennost/sostav-koda-markirovki-legprom) | GTIN14 trong AI(01), serial riêng cho từng đơn vị. |
| S17 | [Честный ЗНАК — Khai size quần áo](https://markirovka.ru/knowledge/tovarnye-gruppy/legkaya-promishlennost/kak-korrektno-ukazat-razmer-odezhdy-v-kartochke-tovara-legprom) | Ngày 13.05.2025; giữ hệ size và giá trị, không tự đổi S/M thành size Nga. |
| S18 | [Честный ЗНАК — Sửa thuộc tính bắt buộc](https://markirovka.ru/knowledge/tovarnye-gruppy/obschie-voprosy-gis/vnesenie-izmeneniy-v-atributy-kartochek-tovarov-v-kmt) | Ngày 03.08.2026; quy trình yêu cầu sửa, có biểu mẫu quần áo và đồ lót. |
| S19 | [Честный ЗНАК — Phạm vi thí điểm tự sửa](https://markirovka.ru/knowledge/tovarnyegruppy/obschie-voprosy-gis/kak-sformirovat-shablon-zayavki-na-vnesenie-izmeneniy-v-obyazatelnye-atributy-v-kartochke) | Ngày 19.08.2026; danh sách nhóm thí điểm không gồm quần áo; loại trừ trường TNVED và thành phần. |
| S20 | [WB — Xác minh mã định danh](https://seller.wildberries.ru/instructions/ru/ru/material/verify-product-identifiers?categoryId=labeling-items-and-orders-in-fbs%5C&goBackOption=prevRoute) | Kiểm tra GTIN trong NC/GS1 và trạng thái; khác với kiểm checksum cục bộ. |
| S21 | [WB — Cách bán hàng có КИЗ](https://seller.wildberries.ru/instructions/ru/ru/material/how-to-sell-cim-labeled-items) | Cập nhật 25.09.2026; cùng mã có thể chứa cả hàng phải và không phải маркировка. |
| S22 | [WB API — Bổ sung GTIN](https://t.me/wb_api_notifications/479) | Thông báo chính thức về trường gtin và skus; không suy đoán nesting từ thông báo. |
| S23 | [WB — Giải đáp GTIN tồn kho](https://t.me/wbsellerofficial/6808) | Xử lý tình huống cùng GTIN đã có và Дополнительный GTIN; không mở quyền tạo hàng mới chung GTIN. |
| S24 | [Честный ЗНАК — Kiểm tra tài liệu phù hợp](https://markirovka.ru/knowledge/tovarnye-gruppy/legkaya-promishlennost/proverka-razreshitelnykh-dokumentov-pri-vvode-v-oborot-s-1-sentyabrya-2026-goda) | Ngày 04.08.2026; yêu cầu tài liệu khi đưa vào lưu thông, tách với phân loại TNVED. |
| S25 | [Честный ЗНАК — Mô tả màu và thuộc tính](https://markirovka.ru/community/shoes-and-clothes/samye-populyarnye-voprosy-po-opisaniyu-kartochek-tovarov-pri-markirovke-odezhdy) | РАЗНОЦВЕТНЫЙ chỉ sản phẩm đa màu thực tế; lấy từ điển đúng nhóm. |
| S26 | [GS1 — Biểu diễn GTIN trong trường 14 ký tự](https://support.gs1.org/support/solutions/articles/43000734355-what-is-the-required-format-of-gtin-in-gs1-edi-standards-) | Cập nhật 02.09.2024; thêm số 0 bên trái trong biểu diễn 14 ký tự khi định dạng đích yêu cầu. |
| S27 | [GS1 — Cách tính check digit](https://www.gs1.org/services/how-calculate-check-digit-manually) | Thuật toán trọng số 3/1 và chữ số kiểm tra cho các định dạng GTIN. |
