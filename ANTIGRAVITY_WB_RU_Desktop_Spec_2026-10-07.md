# Đặc tả phần mềm desktop kiểm tra và đăng sản phẩm Wildberries tại Nga

**Dành cho:** Antigravity và nhóm phát triển ứng dụng của chủ dự án.  
**Phiên bản:** 1.0 — ngày 07/10/2026 theo UTC+7.  
**Mốc kiểm chứng nguồn:** 06/10/2026 UTC.  
**Thị trường trọng tâm:** người bán tại Nga, bán trong Nga; giai đoạn đầu tập trung quần áo và hàng dệt thuộc nhóm легкая промышленность.  
**Ngôn ngữ giao diện dự kiến:** tiếng Việt, giữ tên trường và thuật ngữ tiếng Nga để đối chiếu trên hệ thống gốc.  
**Trạng thái:** đặc tả để triển khai; chưa phải một phần mềm đã xây dựng hoặc một API contract đã thử với tài khoản thật.

Ứng dụng phải giúp người bán tìm và sửa lỗi trong thẻ sản phẩm Wildberries đã có, đặc biệt ТН ВЭД, giới tính, biến thể, GTIN và trường bắt buộc; đồng thời tạo thẻ mới bằng một quy trình có kiểm tra. Việc đối chiếu phải kết nối được danh tính hàng thật với dữ liệu WB, GS1, Национальный каталог và Честный ЗНАК. Mỗi kết luận cần cho biết dữ liệu nào đã được xác minh, dữ liệu nào còn thiếu và hành động nào thực sự hoàn tất.

Tài liệu này tự chứa kiến thức nghiệp vụ, yêu cầu sản phẩm, thiết kế dữ liệu, quy tắc kiểm tra, quy trình tích hợp, tiêu chí nghiệm thu và nguồn chính thức. Antigravity không cần dựa vào cuộc trò chuyện trước đó. Các mã nguồn như [WB01], [CH02], [NC01] dẫn đến tài liệu chính thức trong phần 20.

## Mục lục

1. Chỉ dẫn triển khai cho Antigravity
2. Mục tiêu và phạm vi sản phẩm
3. Thuật ngữ và các lớp dữ liệu phải tách riêng
4. Quy định và giới hạn nghiệp vụ
5. Dữ liệu cần thu thập và nguyên tắc đối chiếu
6. Mô hình dữ liệu nội bộ
7. Bộ máy kiểm tra và danh mục quy tắc
8. Quy trình kiểm tra và sửa thẻ cũ
9. Quy trình tạo thẻ mới
10. Tích hợp WB và các hệ thống marking
11. Đồng bộ và cơ chế ghi thay đổi an toàn
12. Thiết kế giao diện
13. Kiến trúc desktop và bảo vệ dữ liệu
14. Vai trò của AI và OCR
15. Ví dụ dữ liệu và thuật toán
16. Bộ tình huống nghiệm thu
17. Kế hoạch triển khai và định nghĩa hoàn thành
18. Điểm cần xác minh trước khi bật chức năng ghi
19. Chỉ dẫn bắt đầu công việc cho Antigravity
20. Nguồn chính thức và phạm vi sử dụng

## 1 Chỉ dẫn triển khai cho Antigravity

### 1.1 Việc phải làm đầu tiên

Đọc toàn bộ tài liệu trước khi thiết kế module hoặc sửa code. Kiểm tra repository hiện có, công nghệ giao diện, cơ sở dữ liệu, cơ chế đóng gói, cách quản lý token, tài liệu dự án và phần đã triển khai. Giữ cấu trúc và công nghệ hiện có nếu chúng đáp ứng yêu cầu. Không mặc định đây là dự án mới và không thay toàn bộ ứng dụng chỉ để dùng một framework khác.

Lập bảng đối chiếu giữa chức năng hiện có và yêu cầu trong tài liệu. Phân biệt rõ phần đã chạy được, phần chỉ có mock, phần cần contract API, phần phải thao tác bằng cổng chính thức. Nếu chưa có repository, đề xuất kiến trúc tối thiểu trước khi tạo dự án; hệ điều hành và framework là quyết định triển khai, không phải yêu cầu pháp lý.

### 1.2 Cách đọc các yêu cầu

| Nhãn | Ý nghĩa | Cách thực hiện |
| --- | --- | --- |
| QUY ĐỊNH ĐÃ KIỂM CHỨNG | Yêu cầu hoặc hành vi được nguồn chính thức hỗ trợ tại mốc kiểm chứng | Lưu nguồn, phạm vi, ngày hiệu lực và phiên bản; kiểm tra lại khi triển khai |
| CHÍNH SÁCH ỨNG DỤNG | Thiết kế do chủ dự án lựa chọn để giảm lỗi và giúp người bán | Có thể cấu hình, nhưng không quảng cáo thành quy định bắt buộc của WB hoặc pháp luật |
| CẦN XÁC MINH CONTRACT | Có khả năng tích hợp nhưng chưa xác nhận đầy đủ schema, quyền hoặc hành vi môi trường đích | Làm adapter và mock; chỉ mở ghi sau khi contract được xác minh |
| CẦN XỬ LÝ NGHIỆP VỤ | Thiếu chứng cứ hoặc cần phân loại, sửa hồ sơ, ký, hỗ trợ từ đơn vị có quyền | Hiển thị việc cần làm và lưu bằng chứng; không tự đoán để vượt qua |

Các từ MUST, SHOULD và MAY trong thiết kế tương ứng với bắt buộc của sản phẩm, nên làm và tùy chọn. MUST trong tài liệu không tự động có nghĩa là pháp luật yêu cầu.

### 1.3 Những nguyên tắc không được phá vỡ

1. Không tạo GTIN bằng cách sinh một chuỗi đúng checksum rồi gọi đó là mã đã đăng ký.
2. Không tự quyết định mã ТН ВЭД đầy đủ từ tiêu đề, ảnh hoặc một từ khóa.
3. Không dùng giới tính của người mẫu làm nguồn xác định đối tượng sử dụng của hàng hóa.
4. Không xóa barcode, size, ảnh hoặc giấy tờ hiện có khi người dùng chỉ yêu cầu sửa một trường khác.
5. Không gọi một thẻ là đã đồng bộ khi chỉ một hệ thống nhận yêu cầu.
6. Không coi HTTP 200, một dòng Excel đã nhập hoặc thẻ đã được tạo là bằng chứng mọi kiểm tra từ xa đã hoàn tất.
7. Không tự ký hồ sơ, đặt mã có phí, đưa vào hoặc rút khỏi lưu thông chỉ vì người dùng chạy chức năng kiểm tra thẻ WB.
8. Không tự thay quốc gia, nhà sản xuất, ngày sản xuất, thành phần hoặc số chứng từ để làm cho biểu mẫu hết báo lỗi.
9. Lỗi kết nối, thiếu quyền và nguồn chưa được kiểm tra phải là UNKNOWN, không được đổi thành PASS hoặc NOT_REQUIRED.
10. Không có endpoint, field, enum hoặc quyền nào được coi là thật chỉ vì AI đã viết một ví dụ có vẻ hợp lý.

## 2 Mục tiêu và phạm vi sản phẩm

### 2.1 Các công việc chính của người bán

| ID yêu cầu | Công việc | Kết quả cần có |
| --- | --- | --- |
| FR01 | Kết nối đúng tài khoản WB | Xác định shop và tổ chức, quyền đọc/ghi, thời hạn xác thực; không lẫn dữ liệu tài khoản |
| FR02 | Nhập danh mục đã đăng | Có toàn bộ thẻ và size trong phạm vi đồng bộ, ảnh chụp dữ liệu gốc, báo cáo trang hoặc bản ghi nhập lỗi |
| FR03 | Kiểm tra thẻ cũ | Danh sách lỗi theo thẻ, biến thể, trường, mức độ, chứng cứ và thao tác bị ảnh hưởng |
| FR04 | Đối chiếu nguồn | Xem WB, hồ sơ hàng thật, dữ liệu GTIN/NK và chứng từ cạnh nhau |
| FR05 | Đề xuất sửa | Có giá trị cũ, giá trị mới, lý do, nguồn và giới hạn tự động hóa |
| FR06 | Sửa một thẻ hoặc hàng loạt | Chỉ ghi thay đổi đã chọn; kiểm tra lại trước ghi và đọc lại sau ghi |
| FR07 | Tạo sản phẩm mới | Trình hướng dẫn tạo mẫu, màu, size, định danh, nội dung, hồ sơ và thẻ WB |
| FR08 | Theo dõi đồng bộ | Tách trạng thái từng hệ thống, từng thẻ, từng tác vụ; phục hồi sau lỗi hoặc khởi động lại |
| FR09 | Xử lý việc cần làm ngoài app | Xuất gói dữ liệu hoặc hướng dẫn đúng cho NK/GS1/đơn vị chứng nhận; nhập lại kết quả |
| FR10 | Lưu lịch sử và xuất báo cáo | Truy vết được ai sửa gì, dựa vào nguồn nào, phiên bản nào và kết quả thực tế |

### 2.2 Phạm vi phiên bản đầu có thể sử dụng

Phiên bản đầu hoàn chỉnh phải bao gồm FR01 đến FR10 cho các danh mục quần áo đã được cấu hình và kiểm chứng. Không được gọi một bảng kiểm tra chỉ đọc là đã hoàn thành mục tiêu của người dùng: cần có luồng cập nhật WB và tạo thẻ mới thực sự sau khi xác minh contract.

Tích hợp NK nên có đọc và đối chiếu khi tài khoản có quyền. Những bước sửa thuộc tính bắt buộc, sửa tại GS1, ký hoặc xuất bản chưa được hỗ trợ trong tài khoản phải có luồng bàn giao có theo dõi. Đây là một phần của quy trình hoàn chỉnh, không được giả lập là đã đồng bộ.

Mỗi module có `supportedScope` và điều kiện loại trừ. Hàng bảo hộ/PPE, y tế hoặc nhóm chuyên biệt chưa được hỗ trợ nhận `UNSUPPORTED_SCOPE` và yêu cầu đánh giá phù hợp; không tự áp bộ quy tắc quần áo thông thường để cho PASS. Các kiểm tra độc lập như định dạng mã và bảo toàn dữ liệu vẫn được chạy.

### 2.3 Phần mở rộng được tách riêng

Các module quản lý đơn, UPD/ЭДО, phát hành mã, in và quét mã từng chiếc, đưa vào lưu thông, rút khỏi lưu thông, hoàn hàng và thay mã có thể phát triển sau. Phiên bản đầu phải lưu được thông tin ảnh hưởng đến các module đó, nhưng không được thực hiện nghiệp vụ có tác động pháp lý chỉ để hoàn tất một bài đăng.

Giá và tồn kho phải là tác vụ riêng với quyền và kiểm tra riêng. Tạo nội dung thẻ không tự động có nghĩa là công bố tồn kho để nhận đơn. Quảng cáo, quyết toán, tối ưu phí và thuế không nằm trong phạm vi đặc tả này.

### 2.4 Chỉ số chất lượng của ứng dụng

Đo tỷ lệ thẻ nhập thành công, số lỗi đã xác minh và đã xử lý, số lỗi UNKNOWN do thiếu nguồn, tỷ lệ tác vụ ghi được đọc lại xác nhận, số xung đột được ngăn chặn và số lần người bán phải làm lại. Không dùng chỉ số “100% hợp pháp” hoặc chỉ đếm số ô đã điền.

Mục tiêu kỹ thuật để benchmark nội bộ: thao tác xem và lọc tập 10.000 thẻ hoặc 50.000 biến thể không làm treo giao diện; tác vụ mạng chạy nền, có tiến độ và hủy phần chưa gửi. Đây là mục tiêu thiết kế để đo trên máy thử, không phải cam kết thời gian xử lý của WB/CRPT.

## 3 Thuật ngữ và các lớp dữ liệu phải tách riêng

| Thuật ngữ | Ý nghĩa trong ứng dụng |
| --- | --- |
| Wildberries hoặc WB | Sàn thương mại điện tử; nơi lưu thẻ bán hàng, size, barcode và trạng thái xử lý |
| Карточка товара | Thẻ sản phẩm; “bài đăng” trong ngôn ngữ của người dùng |
| nmID | Định danh thẻ WB; không phải GTIN hoặc mã từng chiếc |
| imtID | Định danh nhóm liên kết thẻ của WB; giữ riêng với mẫu sản phẩm nội bộ |
| chrtID | Định danh size/biến thể của WB theo contract; không phải mã GTIN |
| Артикул продавца hoặc vendorCode | Mã hàng do người bán đặt; cần quản lý trong phạm vi tài khoản |
| Баркоды hoặc skus | Mã dùng gắn hàng với thẻ/size trên WB; không mặc định mọi mã trong đây là GTIN đã đăng ký |
| GTIN | Mã định danh một loại hàng/biến thể thương mại theo GS1; không mã hóa trực tiếp giới tính hoặc ТН ВЭД |
| Дополнительный GTIN | Trường GTIN bổ sung của WB; phải theo đúng mức dữ liệu và giới hạn API |
| ТН ВЭД ЕАЭС | Hệ thống phân loại hàng hóa của EAEU; mã đầy đủ thường dùng trong quy trình này có 10 chữ số |
| ОКПД2 | Hệ thống phân loại sản phẩm của Nga; không phải tên gọi khác của ТН ВЭД |
| GS1 và ГС1 РУС | Hệ thống/đơn vị quản lý chuẩn và cấp định danh; vai trò cấp mã khác với kiểm tra checksum |
| Национальный каталог hoặc NK | Danh mục mô tả hàng hóa dùng trong hệ thống marking; có chủ thẻ, phiên bản và trạng thái riêng |
| Честный ЗНАК hoặc ЧЗ | Hệ thống marking của Nga; “Makiroka/markirovka” trong yêu cầu được hiểu theo ngữ cảnh này |
| ГИС МТ | Hệ thống thông tin giám sát hàng hóa thuộc diện marking |
| КМ, КИ, СИ, КИЗ | Các thuật ngữ liên quan mã/định danh/phương tiện marking; giữ thuật ngữ gốc của từng API và nhóm hàng, không coi tất cả hoàn toàn đồng nghĩa |
| Data Matrix | Ký hiệu có thể chứa GTIN và định danh riêng từng chiếc cùng dữ liệu kiểm tra; khác barcode logistics |
| РД | Hồ sơ xác nhận phù hợp: ДС, СС, СГР theo trường hợp |
| ДС | Декларация о соответствии, bản công bố phù hợp |
| СС | Сертификат соответствия, giấy chứng nhận phù hợp |
| СГР | Свидетельство о государственной регистрации, chứng nhận đăng ký nhà nước trong trường hợp áp dụng |
| УКЭП và МЧД | Chữ ký điện tử đủ điều kiện và giấy ủy quyền điện tử tương ứng; liên quan quyền ký thực tế |
| FBW/FBO, FBS, DBS | Các mô hình thực hiện đơn; nghĩa vụ giao và xử lý mã khác nhau theo mô hình và giao dịch |

Ứng dụng phải phân biệt ít nhất sáu đối tượng: mẫu hàng; biến thể bán thực tế theo màu và size; thẻ WB; thẻ NK theo GTIN và chủ thể; lô hàng/chứng từ; từng chiếc có mã riêng. Một mẫu có nhiều GTIN; một GTIN có thể xuất hiện hợp pháp ở nhiều nhà bán lẻ; nhiều chiếc cùng GTIN vẫn cần định danh từng chiếc theo quy trình marking. [GS01] [GS04]

## 4 Quy định và giới hạn nghiệp vụ

### 4.1 Thông báo WB về GTIN từ tháng 10 năm 2026

Theo hướng dẫn WB cập nhật 02/10/2026, từ 01/10/2026 WB kiểm tra GTIN qua ГИС МТ đối với người bán Nga bán trong Nga, ở hàng thuộc diện marking. Thẻ cần GTIN và ТН ВЭД; WB nêu việc chặn thẻ khi mã thiếu hoặc không hợp lệ. Có cơ chế GTIN bổ sung cho những tình huống được WB quy định. [WB01]

Ứng dụng phải lưu phạm vi quốc gia của người bán, thị trường đích và nhóm hàng trước khi áp dụng quy tắc. Thông báo dành cho Belarus hoặc Kazakhstan không được áp nguyên trạng cho Nga chỉ vì giao diện cùng tiếng Nga.

Hướng dẫn CRPT về luật nền tảng số mô tả giai đoạn kiểm tra 180 ngày đối với thẻ đã tồn tại. Không dùng thông tin này để hứa mọi thẻ cũ được miễn chặn hoặc để hoãn toàn bộ công việc sửa. Lưu riêng quy định chuyển tiếp và trạng thái xử lý thực tế từ WB. [CH01]

### 4.2 GTIN và barcode không thay thế cho nhau trong mọi ngữ cảnh

Mỗi biến thể thực sự khác màu hoặc size cần danh tính GTIN phù hợp. Người bán lại một hàng đã có GTIN hợp lệ không phải tạo định danh mới chỉ vì đổi người bán. [GS01] [GS04]

Phần mềm phải kiểm tra riêng cấu trúc mã, đăng ký, mô tả hàng ứng với mã và cách mã được gắn vào WB. Prefix GS1 không chứng minh nước sản xuất. [GS02] [GS05]

Hướng dẫn WB ngày 02/10 cho phép không in riêng GTIN khi đã thể hiện phù hợp trong mã marking hoặc đang sử dụng barcode hiện có theo tình huống hướng dẫn. Vì vậy app không được tự đề nghị bóc toàn bộ tem barcode cũ. Nhãn logistics thực tế vẫn phải khớp mapping thẻ và hàng giao. [WB01] [WB05]

### 4.3 ТН ВЭД và giới tính

Phân loại cần loại hàng, kết cấu dệt kim/dệt khác, vật liệu, đối tượng sử dụng và các ghi chú pháp lý áp dụng. Chương 61 và 62 có quy tắc về giới tính và hàng trẻ nhỏ; nhánh 6111/6209 gắn với chiều cao không quá 86 cm. Mã nhóm nam/nữ chỉ là một phần của phép phân loại, không phải bộ suy luận đầy đủ. [TN01] [TN02] [TN03]

Với unisex, phải xem ghi chú 9 của chương áp dụng, kiểu cắt và cấu tạo. Nếu sau khi áp dụng ghi chú vẫn không xác định được hàng thuộc nam/trẻ em trai hay nữ/trẻ em gái, phải phân loại theo quy định về các nhóm bao gồm hàng nữ/trẻ em gái. Điều kiện này không áp tự động cho mọi hàng quảng cáo là unisex và không tự quyết định thuộc tính tiếp thị. [TN02] [TN03]

NK có quy trình bổ sung mã đủ 10 chữ số trước khi đưa vào lưu thông. Việc thay nhóm bốn chữ số và việc bổ sung chi tiết trong cùng nhóm có tác động khác nhau; không được tự kết luận mọi sửa ТН ВЭД đều cần GTIN mới hoặc đều giữ được GTIN cũ. [CH06]

Trong dữ liệu, tách `intendedAudience`, `wbGender`, `nkGender`, `tariffGenderBranch` và `documentAudienceScope`. Các trường này được đối chiếu ngữ nghĩa theo loại hàng; không ép bằng nhau bằng phép so sánh chuỗi.

### 4.4 Giấy tờ và phạm vi sản phẩm

Quần áo người lớn thường đánh giá theo ТР ТС 017/2011; hàng trẻ em theo ТР ТС 007/2011. Loại hồ sơ phụ thuộc loại hàng, độ tuổi, lớp mặc và phương thức chứng nhận. Với người lớn, nhiều hàng lớp hai/ba dùng ДС; một số nhóm như đồ lót, đồ bơi và nhóm được quy định riêng cần СС. Phải xét sửa đổi có hiệu lực, gồm Quyết định 75 từ 26/04/2026. [TR01] [TR02]

Không áp công thức “tất cả trẻ em đều cần СГР”. Các trường hợp trẻ nhỏ, lớp mặc và nhóm sản phẩm trong quy chuẩn 007 phải được đánh giá riêng. [TR03]

Giấy chứng nhận tự nguyện hoặc thư từ chối chứng nhận không được nhập như một СС/ДС bắt buộc để vượt kiểm tra. Nếu hàng thuộc diện miễn, cần căn cứ thật và đúng cơ chế khai báo. [WB02]

Trên WB cần số, loại và dữ liệu hồ sơ theo cơ chế kiểm tra hiện hành. Mã ТН ВЭД hoặc ОКПД2 trong hồ sơ có thể thể hiện phạm vi theo nhóm; mã 4/6 số không được coi là sai chỉ vì khác chuỗi mã 10 số của biến thể. Cần kiểm tra tên hàng, nhà sản xuất, model, nhãn hiệu, phụ lục và lô. [WB02]

Đối với quy trình đưa hàng dệt vào lưu thông, CRPT áp dụng kiểm tra thông tin РД trong thẻ và hồ sơ khai báo theo các mốc tháng 3 và tháng 9/2026, khác nhau theo sản xuất trong Nga, nhập EAEU và nhập ngoài EAEU. Thiếu РД ở NK không được coi là đã giải quyết chỉ vì WB có một file PDF. [CH02]

Thời hạn hồ sơ phải được đánh giá cùng loại serial/batch, ngày sản xuất/đưa vào lưu thông và trạng thái chính thức. Nếu thiếu dữ liệu này, kết quả là cần xem xét; không dùng `endDate < today` làm kết luận chung cho mọi hàng tồn.

### 4.5 GS1 và NK là những bước riêng

Tài khoản có hệ thống cấp GTIN chính theo cách tham gia. Hệ thống cấp mã không tự xác định nơi có quyền sửa mọi thuộc tính của từng thẻ. Việc có mã ở GS1 không chứng minh NK đã nhận và công bố mô tả; GTIN quốc tế cũng có quy trình riêng. [CH04] [CH05]

Nếu thuộc tính đang được quản lý tại GS1, ứng dụng cần ghi rõ “sửa tại GS1 rồi kiểm tra việc nhận tại NK”. Nếu chủ thể có quyền quản lý ở NK, chọn quy trình NK tương ứng. Không tự động ghi hai chiều rồi dùng bản đến sau cùng để thắng xung đột.

Tách `gtinAllocationMaster` của tài khoản với `descriptionSource` và `fieldWriteAuthority` của thẻ/trường. Chọn đường sửa từ nguồn, quyền và trạng thái đã xác minh, không chỉ từ một cờ master cấp mã.

### 4.6 Sửa thẻ NK đã công bố

Thay đổi thuộc tính bắt buộc của thẻ đã công bố phải dùng quy trình được hỗ trợ cho nhóm hàng, trường dữ liệu, trạng thái và chủ thẻ. Hướng dẫn sửa thuộc tính bắt buộc có luồng đề nghị hỗ trợ. Chương trình thử nghiệm tự sửa được công bố tháng 8/2026 không liệt kê quần áo trong các nhóm áp dụng; không được hứa mọi trường giới tính, thành phần hoặc ТН ВЭД của quần áo đều sửa trực tiếp qua API. [CH07] [CH08]

Bổ sung РД theo hướng dẫn nhập NK có thể tạo một phiên bản nháp mới của thẻ đã công bố. Cần theo dõi kết quả nhập và các bước duyệt, ký, công bố tiếp theo; nhập thành công chưa làm phiên bản mới trở thành bản đang có hiệu lực. [CH03] [CH09]

### 4.7 Thẻ sản phẩm và mã từng chiếc

Trạng thái thẻ NK và trạng thái mã từng chiếc là hai lớp riêng. Không suy ra hàng đã ở trạng thái lưu thông từ việc thẻ “Опубликована”. Phân biệt nhà đăng ký GTIN, chủ thẻ, người phát hành mã, chủ sở hữu hiện tại, nhà sản xuất, người nhập khẩu và người bán. Các vai trò có thể thuộc những đơn vị khác nhau một cách hợp lệ. [NC01] [CH15] [CH16]

WB có tín hiệu như `needKiz` và xác nhận `kizMarked` trong các khả năng API đã công bố. Cờ “đã dán marking” chỉ được cập nhật theo thực tế và quy trình được phép; không đặt true chỉ vì mã ТН ВЭД thuộc danh sách cần marking. [API06]

Các bước như báo cáo áp mã, đặt mã, đưa vào lưu thông và xử lý trả hàng phải theo kịch bản nhóm hàng. Không biến một bước của nhóm khác thành nghĩa vụ chung cho mọi quần áo chỉ vì cả hai dùng Data Matrix.

### 4.8 Hàng tồn và mô hình giao hàng

Các đợt mở rộng marking có nhóm và hạn riêng. Cửa sổ hàng tồn đợt 4 năm 2026 không phải cơ chế hợp thức hóa chung cho mọi jeans, hoodie hoặc hàng đã thuộc diện marking từ trước. Quy tắc phải có danh mục, điều kiện và mốc thời gian áp dụng. [CH13] [CH14]

Mốc cấu hình cần đối chiếu cho hàng tồn: đợt 3 đã qua hạn 01/03/2026; đợt 4 cấm bán hàng tồn chưa marking từ 01/08/2026 và có hạn marking/đưa vào lưu thông đến 30/11/2026 cho hàng đủ điều kiện. Không áp mốc sau cùng nếu chưa chứng minh hàng thuộc đúng đợt. [CH13] [CH14]

Với FBW/FBO và FBS, trách nhiệm về mã và chứng từ chuyển giao khác nhau. Tại mốc kiểm chứng có khác biệt diễn đạt giữa hướng dẫn WB và CRPT về thời điểm xử lý rút lưu thông trong một số luồng FBS. Module đó phải có chính sách đã được xác nhận theo đúng kịch bản; chưa xác nhận thì giữ thao tác tự động ở trạng thái BLOCKED_POLICY. Việc này không ngăn audit hoặc sửa nội dung thẻ. [WB07] [CH12]

Nhập hàng từ ngoài EAEU có bước marking trước khi hải quan giải phóng theo kịch bản áp dụng, rồi xử lý chứng từ/nhập lưu thông tương ứng. App lưu riêng nguồn sản xuất, người nhập, tờ khai và trạng thái nghiệp vụ; không tự phát sinh chứng từ nhập khẩu khi đăng thẻ WB. [CH17]

### 4.9 Nội dung nhãn vật lý

Với hàng thuộc quy chuẩn 017, profile kiểm tra nhãn cần xét tên hàng, nước sản xuất, đơn vị chịu trách nhiệm và địa chỉ theo vai trò, size, thành phần, ngày sản xuất, model, hướng dẫn chăm sóc, brand nếu có, thông tin lô khi áp dụng và dấu EAC theo điều kiện quy chuẩn. Hàng trẻ em dùng profile nhãn tương ứng của quy chuẩn áp dụng. [TR04] [TR03]

Nhãn pháp lý, nhãn logistics WB và Data Matrix có mục đích khác nhau. Không kết luận phải in riêng TNVED, GTIN hoặc số chứng từ trên mọi nhãn chỉ vì app lưu những trường đó. Không tự in EAC hoặc claim chứng nhận khi chưa đủ căn cứ cho hàng cụ thể.

## 5 Dữ liệu cần thu thập và nguyên tắc đối chiếu

### 5.1 Bộ dữ liệu tối thiểu

“Bắt buộc” phải được xác định theo thao tác, nhóm hàng, danh mục, vai trò và ngày áp dụng. Bảng dưới là tập dữ liệu app cần quản lý; không có nghĩa mọi dòng đều là trường bắt buộc in lên nhãn hoặc có trong một form WB.

| Nhóm dữ liệu | Trường cần lưu | Nguồn ưu tiên để kiểm chứng |
| --- | --- | --- |
| Tài khoản | Tổ chức, INN, quốc gia, shop ID, thị trường, mô hình bán, vai trò sản xuất/nhập/bán lại | Tài khoản có quyền và hồ sơ tổ chức |
| Danh tính mẫu | Model, mã nhà sản xuất, mã người bán, tên kỹ thuật tiếng Nga, brand | Hồ sơ và nhãn của đúng hàng |
| Cấu tạo | Loại hàng, knitted/woven/other/unknown, lớp mặc, công dụng, kiểu cắt liên quan phân loại | Tài liệu kỹ thuật, mẫu thật, chứng từ xác nhận |
| Đối tượng | Giới tính sử dụng, người lớn/trẻ em, khoảng tuổi hoặc chiều cao khi có | Hồ sơ nhà sản xuất và phạm vi sản phẩm |
| Vật liệu | Từng bộ phận: vải ngoài, lót, ruột; loại sợi và tỷ lệ; trạng thái thiếu dữ liệu | Nhãn thành phần, đặc tả hoặc kết quả thử phù hợp |
| Biến thể | Màu thực, tên màu Nga, size nhãn, hệ size, bảng đo, size Nga, đơn vị bán | Biến thể hàng thật và bảng nhà sản xuất |
| Phân loại | ТН ВЭД 10 số, phiên bản, lý do chọn, ОКПД2 nếu áp dụng, nhóm marking | Nguồn phân loại chính thức và quyết định có căn cứ |
| GTIN | Giá trị gốc, biểu diễn chuẩn, loại nguồn cấp, chủ đăng ký nếu biết, trạng thái xác minh | GS1/NK và hồ sơ quyền sử dụng |
| WB | nmID, imtID, chrtID, subjectID, vendorCode, skus, GTIN bổ sung, required attributes | API hoặc xuất dữ liệu của đúng tài khoản |
| NK | good_id, GTIN, chủ thẻ, nguồn quản lý, các phiên bản, trạng thái, nội dung thuộc tính | API NK hoặc xuất chính thức |
| Hồ sơ РД | Loại, số nguyên bản, ngày đăng ký, ngày hiệu lực/hết hạn khi có, serial/batch, cơ quan/registry, phạm vi và phụ lục | Registry và tài liệu gốc |
| Sản xuất và nhập | Nước sản xuất, nhà sản xuất, địa chỉ pháp lý/cơ sở, nhà nhập khẩu, lô, ngày, chứng từ nhập | Hồ sơ chuỗi cung ứng; không suy từ prefix GTIN |
| Nội dung bán | Tiêu đề, mô tả, ảnh/video, bảng size, đặc tính có thể chứng minh | Dữ liệu mẫu và tài sản được phép sử dụng |
| Thương mại | Giá, tiền tệ, thuế nếu cần cho module, tồn kho theo kho/size, số đo kiện và cân nặng có đơn vị | Hệ thống người bán và phép đo thật |
| Marking từng chiếc | Mã quét nguyên bản, GTIN trích xuất, serial, lô, chủ mã, trạng thái, bằng chứng dán mã | Máy quét và hệ thống có quyền |

### 5.2 Những điểm thường bị bỏ sót khi tạo thẻ

Ứng dụng phải hỏi hoặc nhập đủ: đúng danh mục; tên tiếng Nga đúng hàng; brand đúng; hệ size và size Nga có căn cứ; màu theo biến thể; thành phần theo bộ phận; đối tượng sử dụng; mã phân loại; dữ liệu GTIN; giấy tờ và phạm vi; quốc gia/nhà sản xuất; kích thước kiện và cân nặng có đơn vị. Các thuộc tính bổ sung như mùa, kiểu dáng, loại cạp, đặc tính vật liệu phải đến từ schema và sản phẩm thật.

Tiêu đề có giới hạn riêng; hướng dẫn WB nêu tối đa 60 ký tự. Độ dài mô tả và thuộc tính phải lấy theo danh mục/contract hiện hành. Việc tránh lặp giới tính, thương hiệu hoặc nhồi từ khóa trong tiêu đề có thể là hướng dẫn nội dung hoặc quy ước shop; không gắn tất cả thành lỗi pháp lý. [WB03]

Ảnh cần phản ánh đúng hàng, đủ độ rõ và đúng yêu cầu upload. Có thể dùng 900×1200 hoặc 1200×1600 làm preset nội bộ; không gắn tỷ lệ 3:4 hay preset đó thành yêu cầu pháp luật hoặc điều kiện bắt buộc toàn sàn nếu schema không nói như vậy. [WB04]

### 5.3 Đối chiếu theo ngữ nghĩa

| Dữ liệu | So sánh đúng | So sánh cần tránh |
| --- | --- | --- |
| GTIN | Chuẩn hóa danh tính sau kiểm tra cấu trúc; giữ nguyên bản đầu vào | Cắt số 0 hoặc đổi checksum để hai chuỗi giống nhau |
| Barcode giao kho | Khớp chuỗi thực tế theo mapping WB và lô đang giao | Tự thay bằng dạng GTIN chuẩn hóa chỉ vì có cùng danh tính thương mại |
| ТН ВЭД | Mã đầy đủ, hiệu lực, cấu tạo và phạm vi | Chỉ kiểm tra đủ 10 số hoặc cùng bốn số đầu |
| Giới tính | Từ điển mỗi hệ thống cộng với mục đích sử dụng và nhánh phân loại | So sánh literal “унисекс” với “женский” rồi sửa hàng loạt |
| Tên hàng | Cùng loại, model và đặc tính nhận dạng cần thiết | Bắt tiêu đề SEO giống từng ký tự tên pháp lý |
| Size | Hệ size, giá trị thực, bảng quy đổi của mẫu | Mặc định toàn hệ thống S=42 hoặc L=46 |
| Màu | Alias được xác nhận và màu biến thể thực | Gộp màu đen/trắng vào một biến thể để dùng chung mã |
| Thành phần | Tổng và loại sợi theo từng bộ phận | Cộng vải ngoài 100% với lớp lót 100% rồi báo 200% |
| Hồ sơ | Số nguyên bản, phạm vi, phụ lục, lô, thời điểm và trạng thái | Chỉ `startsWith(TNVED)` hoặc số hồ sơ tồn tại là đủ |

Nguồn ưu tiên phải đánh giá theo trường và chất lượng chứng cứ. Tài liệu mới nhất không tự động đúng hơn nhãn thật; WB và NK cũng có thể cùng chứa dữ liệu sai. Khi hai nguồn đáng tin mâu thuẫn, tạo hồ sơ cần xử lý và không chọn tùy tiện một hệ thống làm đúng tuyệt đối.

### 5.4 Chuẩn hóa an toàn

Lưu mã dưới dạng chuỗi, giữ số 0 đầu. Lưu số đo và đơn vị riêng. Giữ nguyên số chứng từ có chữ Cyrillic và Latin; ký tự nhìn giống nhau không được tự thay cho nhau. Dữ liệu OCR luôn giữ văn bản gốc, vị trí và mức độ tin cậy.

Có thể chuẩn hóa khoảng trắng hiển thị, Unicode hoặc alias trong bản dữ liệu dùng so sánh khi phép chuyển đổi đã xác định không thay ý nghĩa. Không áp chuẩn hóa đó lên Data Matrix, số chứng từ hoặc barcode mà chưa có quy tắc riêng. Mọi thay đổi chuẩn hóa phải có khả năng truy về giá trị gốc.

## 6 Mô hình dữ liệu nội bộ

### 6.1 Các thực thể chính

| Thực thể | Thuộc tính cốt lõi | Ràng buộc |
| --- | --- | --- |
| SellerAccount | id, organizationId, wbSellerId, INN, sellerCountry, market, roles, gtinAllocationMaster, credentialRef | Mọi truy vấn và tác vụ ghi phải có accountId |
| ProductModel | id, manufacturerModel, productType, technicalNameRu, brand, evidenceRefs | Không dùng model ID để thay thế mã của từng biến thể |
| SellableVariant | id, modelId, color, sizeSystem, sizeValue, measurements, currentIdentityVersionId | Một tổ hợp hàng thực; thay danh tính là một quy trình riêng |
| VariantIdentityVersion | id, variantId, gtinIdentityId, attributesHash, scope, validFrom/To, decisionRef | Bất biến; lô/chiếc trỏ đúng phiên bản, không ghi đè danh tính lịch sử |
| ProductClassification | variantId, tnved, tariffVersion, validAt, reasoning, evidence, reviewedBy | Cho phép các size cùng model có phân loại khác khi có căn cứ |
| WbCard | accountId, nmID, imtID, subjectID, vendorCode, additionalGtinRaw, snapshotId | GTIN bổ sung lưu cấp card; không tự tạo sizes[].gtin |
| WbSizeBinding | accountId, nmID, chrtID, variantId, techSize, wbSize, barcodeBindings | Gắn một size WB với đúng biến thể; mơ hồ phải xử lý |
| BarcodeBinding | rawValue, kind, canonicalGtinIfVerified, activeForLogistics, evidence | kind gồm WB_LOGISTICS, REGISTERED_GTIN, UNKNOWN |
| GtinIdentity | raw, canonical14, originalLength, structureStatus, registryStatus, identityEvidence | Cùng mã hợp lệ ở nhiều người bán không tự thành collision |
| NkCard | goodId, gtinIdentityId, ownerId, descriptionSource, fieldWriteAuthority, snapshots, effectiveSnapshotId | Phân biệt bản published đang hiệu lực và bản draft mới |
| ConformityDocument | type, exactNumber, dates, scope, attachments, registrySnapshot, issuanceMode | Ngày không có phải lưu null cùng lý do; không tự đặt giả |
| DocumentCoverage | documentId, variantId/modelId/lotId, scopeDecision, evidenceRefs | Liên kết nhiều hồ sơ và nhiều hàng; quyết định có lý do |
| Lot | id, variantIdentityVersions, production/importDates, origin, supplier, customsRefs, quantities | Ngày và số lượng đi cùng chứng cứ |
| MarkedUnit | id, lotId, variantIdentityVersionId, rawCodeRef, gtin, serial, owner, statusSnapshot | Bảo toàn mã nguyên bản; kiểm soát trùng theo định danh đơn vị |
| Evidence | id, kind, source, fileHash/url, capturedAt, pageOrField, verifiedBy, verificationState | Chỉ OCR hoặc AI chưa đủ thành VERIFIED |
| ExternalSnapshot | system, accountId, entityId, retrievedAt, rawPayload, contentHash, etag | Bất biến; không ghi đè ảnh chụp cũ bằng dữ liệu mới |
| DictionaryVersion | system, category, locale, schemaHash, fetchedAt, validFrom, rawSchema | Dictionary không đọc được không đồng nghĩa không có required fields |
| RuleDefinition | id, version, authority, scope, effectiveDates, sourceRefs, evaluator | Có quy tắc nghiệp vụ và quy tắc bảo vệ hệ thống riêng |
| Assessment | id, scope, operation, rulesetVersion, snapshots, issues, checkedAt | Tái chạy được trên cùng dữ liệu và bộ quy tắc |
| Issue | ruleId, target, fieldPaths, severity, certainty, evidence, suggestedAction | Không lưu chỉ một chuỗi lỗi không cấu trúc |
| ChangeSet | id, accountId, intents, dependencyFingerprints, approval, state | Chỉ chứa thay đổi có mục đích đã xác định |
| SyncJob | id, adapter, operation, payloadHash, remoteId, state, attempts, nextAttemptAt | Hỗ trợ pending, partial, unknown outcome và phục hồi |
| AuditEvent | actor, account, entity, action, before/after refs, reason, timestamp | Không chứa token hoặc mã crypto đầy đủ trong log thường |

### 6.2 Quan hệ phải thực hiện đúng

Một ProductModel có nhiều SellableVariant. WbCard có nhiều WbSizeBinding; mỗi binding cần gắn đúng biến thể. Cấu trúc card theo màu và nhóm trên WB phải lấy từ dữ liệu thật, không ép mọi model thành đúng một nmID. Một biến thể có thể được bán ở nhiều tài khoản với định danh gốc hợp lệ.

Tách tất cả external ID khỏi ID nội bộ. Lưu ID bên ngoài dưới dạng chuỗi trong mô hình lõi; adapter chuyển kiểu theo schema khi gửi. Không join dữ liệu giữa tài khoản chỉ bằng vendorCode hoặc tiêu đề.

Khi một thay đổi danh tính đã được xác nhận, tạo phiên bản/binding mới có phạm vi; lô và mã đã tồn tại vẫn giữ liên kết lịch sử. Cơ chế này không cho phép gán tùy ý nhiều GTIN cho cùng hàng. Các snapshot/phiên bản trong app là mô hình nội bộ, không khẳng định API NK cung cấp truy xuất toàn bộ lịch sử phiên bản.

### 6.3 Dữ liệu quan sát và dữ liệu đã xác minh

Mỗi trường quan trọng nên có một tập giá trị quan sát được thay vì chỉ một cột bị ghi đè liên tục:

```text
FieldObservation
  fieldPath
  rawValue
  normalizedValue
  sourceSystem
  sourceEntityId
  observedAt
  evidenceId
  verificationState = UNVERIFIED | VERIFIED | DISPUTED | OBSOLETE

ResolvedField
  chosenObservationId hoặc explicitValue
  decisionReason
  decidedBy
  decidedAt
  affectedVariantIds
  evidenceAndRuleFingerprint
```

Điều này cho phép người dùng thấy WB ghi “nam”, NK ghi “nữ” và tài liệu nguồn ghi “unisex” mà không đánh mất bất kỳ bản gốc nào.

### 6.4 Các trạng thái sẵn sàng

| Trạng thái độc lập | Câu hỏi được trả lời |
| --- | --- |
| localContentReadiness | Dữ liệu thẻ đã đầy đủ, đúng kiểu và không có lỗi đã biết trong phạm vi kiểm tra chưa? |
| identityReadiness | GTIN và biến thể đã được xác minh từ nguồn nào? |
| documentReadiness | Hồ sơ có được xác minh và bao phủ hàng/lô cho thao tác này không? |
| remoteCardReadiness | WB/NK đã nhận, xử lý và phản hồi trạng thái nào? |
| physicalGoodsReadiness | Hàng thực tế, nhãn và mã từng chiếc đã được kiểm tra cho bán/giao chưa? |
| integrationReadiness | Có đúng quyền, contract, phiên bản và nguồn kết nối đủ mới không? |

Mỗi trạng thái nhận PASS, FAIL, UNKNOWN, PENDING hoặc NOT_APPLICABLE kèm phạm vi. Không gộp chúng thành một dấu xanh “đã chuẩn hoàn toàn”.

## 7 Bộ máy kiểm tra và danh mục quy tắc

### 7.1 Đầu vào và đầu ra

Validator phải là thành phần có thể chạy độc lập với giao diện và mạng. Đầu vào gồm dữ liệu đã nhập, các snapshot, bằng chứng, bộ từ điển, ngày đánh giá, ngữ cảnh người bán và thao tác dự kiến. Connector lấy dữ liệu trước; evaluator không được âm thầm gọi API hoặc ghi dữ liệu.

```text
evaluate(context, observations, resolvedData, dictionaries, ruleset)
  -> issues[]
  -> readinessByOperation
  -> missingEvidence[]
  -> affectedEntities[]
  -> sourceAndVersionManifest
```

Một Issue bắt buộc có: ID ổn định, rule ID và version, entity/variant liên quan, field paths, mức độ, certainty, observed values, expected constraint, nguồn/bằng chứng, lý do bằng tiếng Việt, hành động đề xuất và danh sách thao tác bị chặn. Issue trùng qua nhiều lần chạy phải được gắn lại bằng fingerprint để không tạo hàng trăm việc mới giống nhau.

### 7.2 Mức độ và cách sửa

| Mã | Ý nghĩa |
| --- | --- |
| B | BLOCK: lỗi đã đủ căn cứ hoặc điều kiện kỹ thuật bắt buộc chưa được đáp ứng cho một thao tác cụ thể |
| M | MANUAL_REVIEW: chưa đủ dữ liệu hoặc cần quyết định nghiệp vụ; không kết luận sai hoặc đúng thay người có căn cứ |
| W | WARNING: khuyến nghị hoặc rủi ro không chặn thao tác theo chính sách hiện tại |
| FORMAT | Chuẩn hóa bản nháp bằng phép chuyển đổi không thay danh tính; vẫn lưu bản gốc |
| COPY | Đề xuất lấy giá trị từ nguồn đã xác minh cho đúng hàng; có diff và kiểm tra phụ thuộc |
| REVIEW | Người có trách nhiệm xác nhận phân loại, phạm vi hoặc danh tính trước khi đưa vào changeset |
| EXTERNAL | Phải sửa hoặc xác nhận ở một hệ thống/quy trình bên ngoài được hỗ trợ |

BLOCK chỉ chặn thao tác bị ảnh hưởng. Nó không cấm đọc dữ liệu, lưu nháp, xuất báo cáo hoặc sửa chính lỗi đó. Một lỗi giấy tờ chưa giải quyết không được làm người bán mất khả năng sửa lỗi chính tả không liên quan trên thẻ cũ. Ngược lại, không dùng quyền sửa mô tả làm đường vòng để công bố tồn kho khi điều kiện hàng hóa chưa đạt.

Trong các dòng ghi M/B: mâu thuẫn đã có chứng cứ xác nhận tạo FAIL/BLOCK; thiếu dữ liệu tạo UNKNOWN/MANUAL_REVIEW. MANUAL_REVIEW chỉ chặn khi dữ liệu thiếu là tiền điều kiện của thao tác đó. Quyết định chặn dựa trên `issue × operation × affectedDependencies`. NOT_APPLICABLE cần có căn cứ về phạm vi, không được suy từ dữ liệu trống.

Các gate nghiệp vụ này không bỏ qua ràng buộc API: trước full update, phải kiểm tra toàn bộ payload theo write schema hiện hành. Nếu API cần thêm trường/hồ sơ cho toàn request, nêu rõ điều kiện đó và giữ tác vụ chờ dữ liệu có căn cứ; không bỏ field hoặc chọn miễn trừ để gửi bằng được.

Tái sử dụng quyết định review còn hợp lệ khi chứng cứ, thuộc tính quyết định và quy tắc áp dụng chưa đổi. Không bắt xác nhận lại unisex, size hoặc phân loại đã được xử lý ở mọi lần quét. Khi fingerprint của các căn cứ đổi, mở lại đúng phần cần đánh giá.

### 7.3 Nguồn dữ liệu và schema

| ID | Điều kiện phát hiện | Kết quả mặc định | Cách xử lý |
| --- | --- | --- | --- |
| R001 | Trường quyết định chỉ có từ AI, OCR chưa duyệt, tiêu đề hoặc suy luận không có chứng cứ | M | REVIEW; nêu đúng chứng cứ còn thiếu |
| R002 | Không xác định được quốc gia, nhóm hàng, ngày áp dụng hoặc ruleset đủ mới | M | Chặn kết luận sẵn sàng cho phần chưa xác định; vẫn chạy các kiểm tra độc lập |
| R003 | Thiếu hoặc sai kiểu trường mà schema danh mục hiện hành yêu cầu | B | FORMAT/COPY nếu đủ nguồn; metadata lỗi hoặc mất kết nối phải báo UNKNOWN |
| R004 | Danh mục WB mâu thuẫn loại hàng thật hoặc việc đổi danh mục làm thay required fields | M; B khi sai đã xác minh | REVIEW; tải lại schema; không giả định update API đổi được subjectID |

### 7.4 ТН ВЭД và đối tượng sử dụng

Các kiểm tra sau dùng danh mục thuế quan và ghi chú chương tương ứng, không chỉ danh sách mà WB chấp nhận. [TN01] [TN02] [TN03]

| ID | Điều kiện phát hiện | Kết quả mặc định | Cách xử lý |
| --- | --- | --- | --- |
| R010 | Trường cần mã đầy đủ nhưng không có đúng 10 chữ số | B | FORMAT chỉ bỏ dấu cách phân nhóm hợp lệ; không nối số 0 |
| R011 | Mã đủ 10 số nhưng không có hiệu lực trong tariff version áp dụng | B hoặc M nếu nguồn chưa đủ | REVIEW; mã kế nhiệm chỉ là ứng viên |
| R012 | Chương 61/62 mâu thuẫn kết cấu vật liệu đã xác nhận | M/B | REVIEW; “co giãn” hoặc “cotton” không đủ để xác định knitted/woven |
| R013 | Nhánh mã không phù hợp loại/cấu tạo/công dụng của hàng | M/B | Yêu cầu dữ liệu quyết định nhánh và giải thích ứng viên |
| R014 | Phân nhóm vật liệu mâu thuẫn thành phần hoặc chưa đủ căn cứ cho vật liệu hỗn hợp | M/B | Không dùng quy tắc sợi nhiều nhất cho mọi trường hợp |
| R015 | Đối tượng sử dụng đã xác định mâu thuẫn với WB, NK, nhãn hoặc nhánh phân loại liên quan | M/B | COPY/REVIEW theo bằng chứng; không suy từ ảnh mẫu |
| R016 | Hàng unisex/chưa rõ nhánh giới tính và chưa có quyết định hợp lệ cho chứng cứ hiện tại | M | REVIEW theo ghi chú và hàng thật; tái dùng quyết định còn hiệu lực |
| R017 | Dải size đi qua ngưỡng trẻ nhỏ chưa được đánh giá riêng, hoặc nhánh 6111/6209 không tương ứng chiều cao áp dụng | M/B | Tách đánh giá theo biến thể; không báo lại chỉ vì dải size đi qua ngưỡng |
| R018 | Tuổi sử dụng, danh mục trẻ em/người lớn và quy chuẩn áp dụng mâu thuẫn | M/B | Không suy tuổi chỉ từ S/M/L hoặc một số đo |
| R019 | Đề xuất đổi phân nhóm trên hàng đã có mã/lưu thông nhưng chưa có kế hoạch tác động được xác nhận | M | Xác định quy trình sửa và nhu cầu định danh mới trước khi ghi |

### 7.5 Vật liệu và biến thể

| ID | Điều kiện phát hiện | Kết quả mặc định | Cách xử lý |
| --- | --- | --- | --- |
| R020 | Tổng tỷ lệ của một bộ phận khác 100%, thiếu tỷ lệ hoặc số liệu bất khả thi | M/B | Không tự chia lại tổng để được 100%; yêu cầu nguồn xác nhận |
| R021 | Trộn vải ngoài, lót và ruột thành một tổng hoặc không rõ bộ phận | M | Tách cấu trúc sau xác nhận; không đổi chất liệu bằng tên thương mại |
| R022 | Size nhãn, hệ size, size Nga hoặc bảng đo không nhất quán | M/B | Dùng mapping theo mẫu; kiểm tra lại khi đổi bảng size [CH10] [WB06] |
| R023 | Hàng khác màu/size thực sự bị gộp, mapping biến thể mơ hồ hoặc ảnh nhầm biến thể | M/B | Tách binding; giữ lịch sử card và hàng trong kho |
| R024 | Loại Единица/Комплект/Набор hoặc đơn vị bán chưa phù hợp cấu trúc hàng | M | Xác định quy trình của nhóm hàng; không mặc định mọi gói là một bộ [CH11] |
| R025 | Nhãn vật lý thiếu/mâu thuẫn nội dung bắt buộc áp dụng | M/B ở bước hàng sẵn sàng bán/giao | Tạo việc sửa nhãn; không coi các cột nội bộ đều bắt buộc in [TR04] |

### 7.6 GTIN và barcode

| ID | Điều kiện phát hiện | Kết quả mặc định | Cách xử lý |
| --- | --- | --- | --- |
| R030 | Mã khai là GTIN sai ký tự, chiều dài hoặc checksum | B | FORMAT cho biểu diễn tương đương; không tự sửa một chữ số thành mã mới |
| R031 | Mã đúng cấu trúc nhưng chưa xác minh đăng ký và đúng sản phẩm | M | Đối chiếu nguồn; không cho trạng thái “GTIN đã xác minh” |
| R032 | Một GTIN được gắn cho các biến thể thực sự khác danh tính | B khi đủ căn cứ | REVIEW; tránh báo trùng sai chỉ vì nhiều người bán bán cùng hàng |
| R033 | Barcode logistics bị coi là GTIN hoặc gắn GTIN sai với size WB | M/B | Tách loại mã và binding; đối chiếu hàng đang có tem |
| R034 | Dùng GTIN bổ sung sai cấp dữ liệu hoặc ngoài khả năng contract | B cho bản ghi dự kiến | Không tạo field API giả; card nhiều size cần nhiều GTIN bổ sung phải xử lý riêng |
| R035 | Barcode trên hàng/giao kho không nằm trong binding đã xác nhận | B cho giao hàng | Không tự xóa barcode cũ; giữ mapping theo thực tế |
| R036 | Thay đổi danh tính GTIN trong khi có mã đã in, hàng tồn hoặc giao dịch liên quan | M | REVIEW/EXTERNAL; sửa metadata khác với thay hàng thực |

### 7.7 Hồ sơ và nguồn gốc

| ID | Điều kiện phát hiện | Kết quả mặc định | Cách xử lý |
| --- | --- | --- | --- |
| R040 | Thiếu loại РД cần thiết hoặc loại không phù hợp tuổi/lớp/nhóm hàng | M/B | Xác định theo quy chuẩn; không tạo chứng từ hoặc chọn miễn trừ giả |
| R041 | Số, kiểu ngày hoặc ký tự số hồ sơ không khớp nguồn | M/B | COPY nguyên bản đã xác minh; không thay chữ Cyrillic/Latin tự động |
| R042 | Hồ sơ không bao phủ loại hàng, nhà sản xuất, model, brand, vật liệu hoặc lô | M/B | Đọc phụ lục và scope; mã nhóm phù hợp chưa đủ để PASS |
| R043 | Hồ sơ hết hạn/bị đình chỉ/hủy hoặc thiếu lịch sử lô để kết luận | M/B theo thao tác | Xét trạng thái và lịch sử; không kết luận bằng ngày hết hạn đơn độc |
| R044 | WB đã có hồ sơ nhưng NK thiếu bản cần cho thao tác tiếp theo, hoặc chỉ nằm ở draft | M/B cho thao tác phụ thuộc | EXTERNAL hoặc connector được hỗ trợ; theo dõi bản đã công bố |
| R045 | Nước sản xuất/kiểu nhập suy từ INN, IP Nga, prefix GS1 hoặc subaccount | M/B | Dựa nguồn hàng thật; không tự chọn sản xuất tại Nga |
| R046 | Nhà sản xuất/địa chỉ bị tạo giả, thiếu nguồn hoặc lẫn với kho/người bán | M/B | Phân biệt vai trò và loại địa chỉ; yêu cầu xác nhận |

### 7.8 NK và marking

| ID | Điều kiện phát hiện | Kết quả mặc định | Cách xử lý |
| --- | --- | --- | --- |
| R050 | Không có thẻ NK phù hợp hoặc không đủ trạng thái cho thao tác marking dự kiến | M/B | Phân biệt không tìm thấy, không có quyền, chưa công bố và đang xử lý |
| R051 | Sửa thuộc tính bắt buộc của thẻ published nhưng tài khoản/nhóm hàng không có cách sửa được xác minh | B với ghi trực tiếp | EXTERNAL; tạo gói yêu cầu đúng quy trình [CH07] [CH08] |
| R052 | Chọn diện marking hoặc hàng tồn chỉ từ tên danh mục, sai đợt hoặc ngày không có chứng cứ | M/B | Đánh giá nhóm/điều kiện/ngày; không sửa ngày để hợp lệ |
| R053 | Mã từng chiếc có chủ/quyền hoặc trạng thái không phù hợp giao dịch | M/B cho hàng liên quan | Kiểm tra chuỗi chuyển giao và nghiệp vụ tương ứng; không suy từ chủ GTIN |
| R054 | Một định danh đơn vị bị dùng cho hai chiếc, mã không khớp biến thể hoặc mã hoàn hàng chưa xử lý | B/M | Dừng nghiệp vụ chiếc/lô; không in lại mã cho chiếc thay thế |
| R055 | `kizMarked=true` chỉ do app suy luận, chưa có căn cứ xác nhận hàng đã được dán mã | B với đề xuất đó | Yêu cầu xác nhận thực tế; không bật mặc định |
| R056 | Quy tắc rút/đưa lại lưu thông cho kịch bản chưa được xác minh hoặc nguồn hướng dẫn xung đột | B cho tự động hóa nghiệp vụ đó | BLOCKED_POLICY; audit và sửa nội dung tiếp tục được |
| R057 | Muốn đặt KM bằng GTIN quốc tế nhưng chỉ có thẻ của đơn vị khác, chưa xác nhận quyền/quy trình own-card | M/B cho đặt mã | EXTERNAL; xử lý thẻ thuộc tài khoản nhập khẩu với cùng GTIN phù hợp, không tạo mã mới để né quyền [CH15] |

### 7.9 Nội dung và bảo vệ thao tác

| ID | Điều kiện phát hiện | Kết quả mặc định | Cách xử lý |
| --- | --- | --- | --- |
| R060 | Tiêu đề/mô tả/thuộc tính vượt giới hạn hoặc sai kiểu của contract hiện hành | B | FORMAT/COPY có kiểm tra; quy ước viết SEO riêng thường chỉ W |
| R061 | Nội dung nêu đặc tính không có nguồn hoặc mâu thuẫn hàng thật | M/B | Bỏ hoặc xác minh claim; AI không tự thêm chất liệu, công nghệ hoặc chứng nhận |
| R062 | Media sai giới hạn upload hoặc không tương ứng biến thể | B/M | Kiểm tra kỹ thuật và nội dung riêng; preset ảnh chỉ là chính sách nội bộ |
| R063 | Giá, kho, tồn, số đo kiện hoặc đơn vị thiếu/sai cho thao tác thương mại dự kiến | M/B | Không đoán kích thước/cân nặng; không gửi giá/tồn như một phần sửa TNVED |
| R064 | Tác vụ dùng sai account, thiếu quyền, credential hết hạn hoặc sai môi trường | B | Dừng tác vụ đó; không đổi tài khoản tự động |
| R065 | Endpoint/schema/null semantics chưa xác minh hoặc không thể bảo toàn trường hiện có | B với ghi | CẦN XÁC MINH CONTRACT; không gửi payload thử vào production |
| R066 | Giá trị đích hoặc phụ thuộc đã đổi sau lúc lập bản sửa | B với bản sửa cũ | Đọc lại, dựng diff mới; thay đổi read-only không tự thành xung đột |
| R067 | Kết quả gửi không rõ, xử lý còn pending hoặc chỉ một phần batch thành công | M/PENDING | Đối soát từng mục; không retry mù hoặc báo tất cả thành công |
| R068 | WB đã sửa nhưng nguồn quản lý, bản NK hiệu lực hoặc hàng đã dán tem vẫn khác | M/W theo tác động | Giữ việc đồng bộ còn mở; nêu chính xác phần đã hoàn thành |
| R069 | Muốn bỏ qua một lỗi bằng đánh dấu PASS hoặc sửa ruleset không có lịch sử | B với hành động đó | Không đổi sự thật; ngoại lệ chỉ cho policy có cấu hình waivable và có lịch sử |

Chỉ rule chính sách ứng dụng được đánh dấu `waivable` mới có thể nhận ngoại lệ vận hành. Ngoại lệ không vượt quyền, contract chưa xác minh hoặc mâu thuẫn danh tính, và không đổi FAIL thành PASS. Ngoại lệ pháp lý thực sự phải có căn cứ/điều kiện nguồn; bổ sung chứng cứ để giải quyết MANUAL_REVIEW là xử lý vấn đề, không phải miễn kiểm tra.

### 7.10 Điều kiện chặn theo thao tác

| Thao tác | Chính sách |
| --- | --- |
| SAVE_LOCAL_DRAFT | Cho lưu dữ liệu còn thiếu cùng issues; không chuyển UNKNOWN thành PASS |
| EXPORT_AUDIT | Luôn cho xuất trong quyền truy cập; che dữ liệu nhạy cảm theo tùy chọn |
| APPLY_WB_FIX | Chặn lỗi của trường sửa và các phụ thuộc; không yêu cầu toàn bộ shop hết lỗi |
| CREATE_WB_CONTENT | Kiểm tra contract, required fields, danh tính và phạm vi đã cấu hình; hàng marking cần quy trình GTIN/ТН ВЭД phù hợp; nháp thiếu dữ liệu vẫn lưu cục bộ |
| UPDATE_NK | Cần quyền, trạng thái, field editability và contract; sửa mandatory published chỉ qua luồng được hỗ trợ |
| SIGN_NK | Cần đúng người ký/quyền, nội dung hiển thị và hash nội dung chính xác |
| ACTIVATE_SALES_OR_STOCK | Kiểm tra giá/kho/size và điều kiện hàng hóa của mô hình; là lựa chọn riêng với tạo thẻ |
| ISSUE_OR_CIRCULATE_CODES | Chỉ trong module riêng đã được triển khai, kiểm chứng và cho phép theo hành động cụ thể |

App phải thể hiện rõ đâu là điều kiện của API, đâu là chính sách chặn do sản phẩm lựa chọn. Không mô tả các gate nội bộ thành quy định WB nếu nguồn không xác nhận.

## 8 Quy trình kiểm tra và sửa thẻ cũ

### 8.1 Nhập và kiểm tra toàn bộ danh mục

1. Kết nối, xác định tổ chức/shop, thị trường và quyền. Người dùng chọn audit thẻ đang hoạt động, thùng rác hoặc cả hai; app không tự phục hồi thẻ.
2. Tải đủ các trang trong phạm vi bằng cursor; lưu mốc bắt đầu, mốc kết thúc, số trang, số thẻ, số size, lỗi và cursor phục hồi. “Toàn bộ” chỉ được hiển thị khi đã hoàn tất phạm vi đó.
3. Lưu snapshot WB trước chuẩn hóa. Tải danh mục, schema và các từ điển liên quan; mỗi category giữ phiên bản riêng.
4. Nhập file sản phẩm/chứng từ nếu có. Preview mapping cột, phát hiện mất số 0, mã ở scientific notation, ngày mơ hồ và dòng trùng. Không ghi từ file lên WB ngay.
5. Liên kết thẻ/size với biến thể nội bộ. Ưu tiên ID và mapping đã xác nhận; tên giống nhau chỉ tạo ứng viên.
6. Đối chiếu GTIN/NK và hồ sơ trong quyền của tài khoản. Lưu cả phiên bản đang công bố và phiên bản đang sửa; nguồn chưa đọc được phải hiện rõ.
7. Chạy ruleset; tạo báo cáo theo thẻ, biến thể, loại lỗi và hệ thống phải sửa. Hiển thị riêng lỗi chắc chắn, việc cần xác minh và khuyến nghị nội dung.

### 8.2 Chọn đúng nơi cần sửa

| Tình huống | Hành động của app |
| --- | --- |
| WB sai, NK và chứng cứ hàng thật đúng | Đề xuất sửa WB; bảo toàn mapping/giấy tờ/size và kiểm tra các phụ thuộc |
| WB đúng, NK sai | Giữ WB, lập việc sửa đúng nguồn GS1/NK; chỉ đánh dấu đồng bộ khi đọc lại bản có hiệu lực |
| WB và NK cùng sai | Dựa chứng cứ hàng thật; lập kế hoạch sửa nguồn quản lý và WB theo thứ tự, đánh giá tem/hàng tồn |
| WB và NK khác nhau, không đủ chứng cứ chọn đúng | Tạo hồ sơ cần xác minh, nêu câu hỏi cụ thể; không lấy một bên làm đúng mặc định |
| GTIN ứng với một màu/size khác | Chặn gắn nhầm; tìm đúng mã đã đăng ký hoặc quy trình cấp mã hợp lệ |
| Thẻ nhiều size cần các phân loại/GTIN không biểu diễn được bằng contract hiện tại | Báo giới hạn, đưa phương án tổ chức lại thẻ để người có trách nhiệm đánh giá; không tự chia/xóa thẻ |
| Danh mục WB sai nhưng chưa xác minh API đổi danh mục | Chuẩn bị nội dung/mapping đúng và hướng xử lý trong portal; không gửi subjectID giả vào update |
| Barcode cũ đã gắn hàng trong kho | Giữ nguyên binding cũ; xét cơ chế thêm mã/GTIN bổ sung được hỗ trợ, không giả lập thao tác xóa |

### 8.3 Trình sửa một lỗi

Màn hình phải cho thấy giá trị WB hiện tại, giá trị từ NK, bằng chứng gốc và giá trị đề xuất. Ví dụ thông báo: “Giới tính ở WB là Мужской. Hồ sơ nhà sản xuất đã xác nhận mẫu này dành cho nữ. Cần kiểm tra nhánh ТН ВЭД liên quan trước khi áp dụng. Chưa có kết quả cho phụ lục chứng từ.”

Người dùng có thể chấp nhận giá trị có căn cứ, chỉnh đề xuất hoặc tạo việc hỏi nhà cung cấp. Nút “tự sửa” chỉ áp dụng các phép FORMAT đủ điều kiện. Đề xuất COPY có nguồn được gom thành changeset; thay đổi danh tính hoặc quyết định phân loại phải được review.

### 8.4 Sửa hàng loạt

Nhóm theo quy tắc, nguồn xác nhận và phép sửa, không chỉ theo từ khóa. Trước gửi, hiển thị số thẻ/size, trường sẽ đổi, số mục bị loại vì thiếu chứng cứ, hệ thống đích và những dữ liệu không thuộc endpoint đó.

Một lần duyệt changeset có thể bao phủ cả lô với ý định và phạm vi rõ ràng; không buộc người dùng duyệt lại từng yêu cầu mạng vô hại. Những mục phát sinh xung đột hoặc khác điều kiện phải tách ra. Sau xử lý hiển thị thành công, thất bại, pending, xung đột và chưa gửi theo từng mục.

### 8.5 Sửa dữ liệu quản lý tại GS1 hoặc NK

Gói xử lý phải có GTIN, good_id nếu có, chủ thẻ, trạng thái/phiên bản, trường cũ/mới, tên hàng bị ảnh hưởng, lý do, bằng chứng và tình trạng mã/hàng đã lưu thông. Chọn template chính thức hiện hành của đúng nhóm hàng. App có thể điền template sau khi người dùng đã cung cấp/cho truy cập đúng template, nhưng không tự tạo một bảng tùy ý rồi gọi là template chính thức.

Nếu bước tiếp theo cần cổng GS1, NK, hỗ trợ hoặc chữ ký ngoài app, tạo task có người phụ trách và trạng thái chờ. Chỉ người dùng chủ động gửi mới phát sinh việc gửi cho bên ngoài. Nhập receipt/kết quả và đọc lại nguồn để hoàn tất.

## 9 Quy trình tạo thẻ mới

### 9.1 Trình hướng dẫn tạo sản phẩm

| Bước | Dữ liệu và kiểm tra | Kết quả |
| --- | --- | --- |
| 1 Chọn ngữ cảnh | Shop, thị trường, mô hình bán, vai trò và nguồn hàng | Ngữ cảnh áp dụng quy tắc |
| 2 Mô tả hàng thật | Loại, model, brand, cấu tạo, đối tượng, thành phần, nhà sản xuất | Bản mô tả có bằng chứng, không chỉ nội dung SEO |
| 3 Tạo ma trận biến thể | Màu, hệ size, size thực, quy đổi Nga và đơn vị bán | Danh sách biến thể thực sự cần bán |
| 4 Chọn danh mục và phân loại | subject WB, schema; ứng viên ТН ВЭД, dữ liệu còn thiếu, quyết định có căn cứ | Phân loại đã được xác nhận hoặc bản nháp cần xử lý |
| 5 Kiểm tra định danh | GTIN gốc nếu có; quyền sử dụng và danh tính; cấp mã qua hệ thống có quyền nếu cần | GTIN đúng từng biến thể, không sinh mã giả |
| 6 Hoàn thiện NK | Tạo/nhập mô tả từ nguồn quản lý, hồ sơ, theo dõi duyệt/ký/công bố khi áp dụng | Snapshot NK và việc còn chờ |
| 7 Hoàn thiện hồ sơ | Loại, số, ngày, phạm vi, phụ lục, nhà sản xuất/lô | Liên kết hồ sơ với đúng hàng |
| 8 Soạn nội dung WB | Tên, mô tả, thuộc tính, ảnh, size, số đo kiện | Preview tiếng Nga và các lỗi nội dung |
| 9 Kiểm tra trước gửi | Ruleset, contract, mapping GTIN/barcode, quyền, dữ liệu bắt buộc | Changeset tạo thẻ có thể xem lại |
| 10 Tạo và xác minh | Gửi create, theo dõi lỗi, đọc lại ID/card/size và các trạng thái | Kết quả tạo từng thẻ; không báo bán được chỉ vì có nmID |
| 11 Media và thương mại | Tác vụ ảnh/giá/tồn theo endpoint và quyền tương ứng | Trạng thái riêng của nội dung và kích hoạt bán |
| 12 Hàng và marking | Kiểm tra nhãn, tem và điều kiện giao/bán của hàng thực tế | Hàng sẵn sàng theo mô hình hoặc danh sách việc còn thiếu |

Đây là thứ tự điều phối đề xuất. Một thẻ nháp cục bộ có thể được tạo trước khi có GTIN hoặc mã từng chiếc. Việc chuẩn bị mô tả WB không tự yêu cầu đã hoàn tất phát hành mã cho toàn bộ lô. Ngược lại, app không được tự xác nhận hàng đã dán mã hay tự mở bán để đi qua một bước đang thiếu.

Trước create, từng màu/size cần binding GTIN đúng theo cơ chế barcode/size mà contract Nga hỗ trợ. GTIN bổ sung cấp card không thay cho mapping từng size; không nhân bản một mã cấp card sang S/M/L. Với GTIN quốc tế, đọc thấy thẻ của đơn vị khác không cấp quyền đặt KM; xử lý theo quy trình own-card khi áp dụng. [CH15]

### 9.2 Ví dụ ma trận biến thể

Một model có hai màu đen/trắng và ba size thực S/M/L tạo sáu biến thể. Mỗi biến thể cần định danh GTIN phù hợp; nếu mỗi biến thể có mười chiếc thì có sáu loại hàng và sáu mươi chiếc cần được quản lý theo quy trình mã đơn vị. Đây là ví dụ cấu trúc dữ liệu, không phải sáu mã cấp sẵn để dùng trên hàng thật. [GS01]

Nếu sản phẩm thật là một size duy nhất được nhà sản xuất mô tả dưới dạng khoảng size, app lưu một biến thể cùng hệ/giá trị size đúng. Không tự chia một size thật thành nhiều hàng; cũng không gộp nhiều size thật chỉ vì người dùng nhập “универсальный”.

### 9.3 Nội dung tiếng Nga

Tạo tiêu đề ngắn, gọi đúng loại hàng và đặc tính đã xác minh. Mô tả có thể giải thích form dáng, vật liệu, tình huống sử dụng, chăm sóc và bảng đo khi có nguồn. Trường giới tính, màu, size và chất liệu phải nằm đúng vị trí mà schema yêu cầu; không coi việc viết trong mô tả là đã điền thuộc tính bắt buộc.

Quy ước shop như không đưa giới tính/chất liệu/màu vào tiêu đề phải nằm trong cấu hình biên tập riêng. Không hardcode quy ước đó như quy định bắt buộc với mọi người bán. Không tự thêm “100% cotton”, công dụng y tế, chứng nhận, nước sản xuất hoặc tên brand chỉ để bài hấp dẫn.

### 9.4 Kết quả người dùng nhìn thấy

Ứng dụng phải trả lời được: đã tạo những thẻ nào; còn size nào chưa gắn; những lỗi nào WB trả về; hồ sơ đang chờ kiểm tra ở đâu; media, giá và tồn kho đã gửi chưa; nguồn NK đã ở phiên bản nào; hàng thực tế còn việc gì. Mỗi bước pending có thời điểm kiểm tra lại và đường dẫn hoặc hướng xử lý phù hợp.

## 10 Tích hợp WB và các hệ thống marking

### 10.1 Chính sách xác minh contract

Mỗi connector cần một CapabilityManifest có: môi trường, host, method, path, schema request/response, kiểu xác thực, quyền, nhóm quota, pagination, semantics cập nhật, cơ chế lỗi, hành vi bất đồng bộ, giới hạn batch, ngày/nguồn kiểm chứng và kết quả contract test.

Tách ba khía cạnh: tài liệu có xác nhận tính năng; adapter đã triển khai/test đến đâu; tài khoản hiện tại có quyền gì. Đọc được một trang mô tả API không tự động đặt `productionWriteEnabled=true`.

Tại mốc lập tài liệu, trang dev WB Nga có lúc trả lỗi truy cập. Có thông báo chính thức và trang mirror do WB vận hành để tham khảo, nhưng chưa gọi API vào tài khoản thật. Antigravity phải xác minh phiên bản Nga trước khi cho phép ghi. Không dùng host `.cn` như một fallback tự động cho shop Nga.

### 10.2 Các phương thức WB nền tảng

Các đường dẫn sau có trong tài liệu/hướng dẫn chính thức WB. Dùng contract hiện hành của Nga; host Content được cấu hình là `https://content-api.wildberries.ru` khi được tài liệu môi trường đó xác nhận. [API01] [API07]

| Chức năng | Method và path | Ghi chú triển khai |
| --- | --- | --- |
| Đọc thẻ | POST `/content/v2/get/cards/list` | Cursor; phạm vi active |
| Đọc thùng rác | POST `/content/v2/get/cards/trash` | Phạm vi riêng |
| Tạo thẻ | POST `/content/v2/cards/upload` | Bất đồng bộ |
| Tạo và ghép | POST `/content/v2/cards/upload/add` | Chức năng riêng |
| Cập nhật thẻ | POST `/content/v2/cards/update` | Dựng payload theo write schema |
| Đọc lỗi | POST `/content/v2/cards/error/list` | Gắn lỗi đúng tác vụ/bản ghi |
| Nhóm ngành cha | GET `/content/v2/object/parent/all` | Cache có phiên bản |
| Danh mục | GET `/content/v2/object/all` | Không hardcode subject ID |
| Thuộc tính | GET `/content/v2/object/charcs/{subjectId}` | Required/type/cardinality |
| Giới tính | GET `/content/v2/directory/kinds` | Giá trị từ điển |
| Màu | GET `/content/v2/directory/colors` | Alias nội bộ tách riêng |
| Quốc gia | GET `/content/v2/directory/countries` | Không suy từ prefix mã |
| TNVED theo danh mục | GET `/content/v2/directory/tnved` | Không thay phân loại pháp lý |

Thông báo mới bổ sung các đường dẫn dưới đây; giữ nguyên tiền tố `/api` khi contract yêu cầu, không tự “sửa” cho giống endpoint cũ. [API04]

| Chức năng | Method và path |
| --- | --- |
| Toàn bộ TNVED | GET `/api/content/v2/directory/tnved/all` |
| OKPD2 theo danh mục | GET `/api/content/v2/directory/okpd` |
| Toàn bộ OKPD2 | GET `/api/content/v2/directory/okpd/all` |

Các thao tác media, giá, tồn kho và kho hàng dùng API riêng. Xác minh contract và quyền từng nhóm trước triển khai; không gửi chúng như thay đổi thông thường trong card update. Việc đổi subjectID của thẻ cũ chưa được xác minh trong contract đang đọc, nên phải giữ capability riêng ở trạng thái chưa hỗ trợ cho đến khi có căn cứ.

Lưu metadata như `charcID`, `charcType`, `maxCount`, `required`, `hasFilter`, `isVariable`, `existNamedField` khi contract cung cấp. Dùng ý nghĩa đã được WB mô tả để chọn field có tên riêng hay `characteristics` và xử lý biến thể/nhóm thẻ. Không chỉ lưu tên thuộc tính. Schema đổi phải làm chạy lại form/validation/serialization liên quan. [API09]

### 10.3 Đọc và cập nhật WB

Endpoint cập nhật thẻ ghi đè toàn bộ dữ liệu thuộc phạm vi nó quản lý. Payload phải giữ các trường ghi được cần bảo toàn, gồm các hồ sơ trong `documents` không thay đổi. Không gửi object chỉ có TNVED hoặc nguyên read response chứa field chỉ đọc. Không dùng update để giả lập xóa/thay barcode cũ. [API01] [API03] [API07]

Cursor, filter và điều kiện dừng phải theo phiên bản API đã xác minh. Initial full scan phải bao gồm cả thẻ có/không có ảnh, đọc hết trang và tách trash. Khi quét có thay đổi đồng thời, dùng dedup theo ID, mốc scan và một lượt đối soát phù hợp; không hứa snapshot toàn shop nguyên tử khi API không cung cấp.

Lưu mapping external ID nhận được từ kết quả thật. Sau create/update phải đọc lỗi và đọc lại thẻ; theo dõi riêng việc nội dung xuất hiện và việc kiểm tra hồ sơ kết thúc. Timestamp thay đổi không đủ để chứng minh người bán khác sửa nội dung.

### 10.4 GTIN bổ sung và documents mới của WB

Thông báo WB xác nhận field `gtin` bổ sung và `documents`. Theo thông báo API, dùng GTIN bổ sung khi chính mã đó đã được khai trong `skus` của một thẻ; không khai cùng GTIN vào `skus` của các card/size khác nhau. Kiểm tra mapping và danh tính trước khi chọn cơ chế bổ sung, không dùng nó cho hàng khác hoặc mã chưa xác minh. [API03] [API05]

Bảng dưới là cấu trúc quan sát được trên mirror chính thức, **chỉ để định hướng adapter; chưa phải payload production Nga đã kiểm chứng**. [API02]

| Dữ liệu | Vị trí quan sát được |
| --- | --- |
| GTIN khi create | `variants[].gtin` |
| GTIN khi create/add | `cardsToAdd[].gtin` |
| GTIN khi update | `[].gtin` |
| GTIN khi read | `cards[].gtin` |
| Hồ sơ | `documents.items[]` |
| Thuộc tính hồ sơ | `id`, `type`, `number`, `productNumber`, `tradeName`, `applicant`, `startDate`, `endDate`, `isEndless` |
| Miễn hồ sơ | `documents.excludeDocuments` |
| Kết quả kiểm tra | `documents.items[].verdict`, `documents.overallVerdict` |

`gtin` bổ sung là một giá trị cấp card trong schema quan sát được. Chưa có căn cứ cho `sizes[].gtin`. Đừng tự mở rộng field này để biểu diễn nhiều mã bổ sung cho các size khác nhau.

Adapter phải xác minh enum loại hồ sơ, required fields, ý nghĩa missing/null/empty và trường chỉ đọc. Giữ ID khi sửa hồ sơ hiện có; không mặc định miễn hồ sơ hoặc không có ngày hết hạn. Kết quả kiểm tra có thể cập nhật timestamp của card. [API02] [API03]

Tách đồng hồ đồng bộ nội dung và kiểm tra hồ sơ: hướng dẫn nền nêu khoảng tới 30 phút cho đồng bộ card; tài liệu API mới mô tả kiểm tra hồ sơ tới 3 ngày. Đây là mốc tham khảo cần xác nhận, không phải deadline bảo đảm. Thiếu verdict không thành PASS; tránh update lặp cùng dữ liệu làm khởi động lại kiểm tra. [API07] [API02]

### 10.5 Xác thực WB

Xác định đúng seller qua API thông tin tài khoản, không chỉ tên người dùng nhập. Tài liệu WB có `GET https://common-api.wildberries.ru/api/v1/seller-info`; token được gửi theo cơ chế `Authorization` và cần đúng quyền Content cho nghiệp vụ liên quan. [API08]

Audit dùng quyền đọc khi đủ. Tính năng ghi chỉ bật khi tài khoản có quyền phù hợp. Cách dùng Personal/Service hoặc loại token khác phải phù hợp mô hình phân phối ứng dụng và điều khoản WB hiện hành. Không mặc định một desktop app thương mại phục vụ nhiều người bán luôn được dùng kiểu token dành cho phần mềm nội bộ.

Rate limiter phải nhận cấu hình theo account, loại token, nhóm phương thức và phản hồi API. Không dùng một con số quota duy nhất cho mọi token, endpoint hoặc môi trường. Không gọi ping lặp liên tục để kiểm tra sức khỏe. Decode token chỉ giúp đọc metadata; API mới xác định được quyền và hiệu lực thực tế.

### 10.6 Национальный каталог

Nguồn API NK đã mở trực tiếp là v5.68 ngày 28/09/2026. Các capability dưới đây được tài liệu xác nhận; schema chi tiết và editability phải kiểm chứng với môi trường/tài khoản đích. [NC01]

| Capability | Method và path |
| --- | --- |
| Đọc thẻ own/shared | GET `/v3/feed-product` |
| Đọc published/archive | GET `/v3/product` |
| Danh sách own | GET `/v4/product-list` |
| Phát hiện thay đổi | GET `/v3/etagslist` |
| Schema thuộc tính | GET `/v3/attributes` |
| Cấp GTIN | GET `/v3/generate-gtins` |
| Tạo/sửa | POST `/v3/feed` |
| Kết quả feed | GET `/v3/feed-status` |
| Gửi moderation | GET `/v3/feed-moderation` |
| XML để ký | POST `/v3/feed-product-document` |
| Ký công bố | POST `/v3/feed-product-sign-pkcs` |
| Tra РД theo mã | `/v4/rd-info-by-gtin` — xác minh method/schema trước dùng |

Xác thực dùng `apikey` hoặc GIS MT bearer token. Update cần `good_id`; feed có kết quả bất đồng bộ. GET cấp GTIN và moderation vẫn là mutation. ETag được mô tả cho cache đọc, không chứng minh có khóa ghi `If-Match`. [NC01]

Thiết kế app: không cache hoặc tự retry các GET có tác động; đối soát trước gửi lại. Giữ bản đang hiệu lực khác bản nháp. Adapter trả kết quả từng entry, không gom cả batch thành một boolean. Nếu chưa rõ semantics cập nhật của một thuộc tính, vô hiệu hóa đúng thao tác đó và tạo contract test; phần đọc và audit vẫn hoạt động.

Lưu các trường trạng thái được trả về như `good_status`, `good_detailed_status[]`, `good_signed`, `good_mark_flag`, `good_turn_flag`; không rút gọn chỉ còn published. Bảo toàn trường hợp có bản công bố và bản đang chỉnh. Batch XML/ký hiện giới hạn 10 thẻ; xác nhận lại khi triển khai. `publicationAgreement` liên quan hiển thị website công khai, không tự bật chỉ vì người dùng đồng ý ký. [NC01]

### 10.7 True API và chữ ký

Tài liệu MЧД v11.0 ngày 30/09/2026 mô tả `GET /auth/key` và `POST /auth/simpleSignIn` cho challenge đã ký. Với UUID token, dùng `expireDate` trả về; JWT xử lý hạn theo contract của loại token, không giả định có field này. Token còn chịu hiệu lực của chứng thư/MЧД và quyền đại diện. [NC02]

Luồng ký của ứng dụng phải dùng provider phù hợp đang có ở máy người dùng. Không yêu cầu xuất khóa bí mật. Với tài liệu ký, giữ nguyên bytes/XML của máy chủ; hiển thị nội dung, tính hash, ký đúng bản đó, lưu receipt và không tự sửa XML sau khi ký. Phân biệt cách ký challenge xác thực với chữ ký hồ sơ NK; không dùng một kiểu signature cho mọi endpoint.

Tách các capability `canRead`, `canEdit`, `canAllocateGtin`, `canSign`, `canSubmitCirculation` và `canUseEdo`. Vai trò chỉ đọc không được có nút ghi hoạt động; có chứng thư không đồng nghĩa có quyền cho mọi tổ chức hoặc nhóm hàng. [NC03]

True API có khả năng tra thông tin mã qua `POST /cises/info` và kiểm tra mật mã qua `/cises/check`. Phải xác minh full schema, prefix/base URL, quyền và quota hiện hành trước khi bật connector; không lấy một snippet cũ làm request contract. Tra thẻ NK không thay cho tra trạng thái từng chiếc. [NC04] [NC05]

### 10.8 GS1 và chế độ bàn giao có theo dõi

Connector GS1 phải xác định dịch vụ/tài khoản thực tế. Có tài liệu web service GS46, nhưng không lấy một WSDL cũ rồi mặc định nó hỗ trợ toàn bộ quy trình quần áo marking hiện tại. Giữ capability đọc/ghi ở trạng thái cần xác minh cho đến khi có contract đúng dịch vụ. [GS06]

Khi quản lý mô tả tại GS1, app hỗ trợ xuất dữ liệu cần sửa, ghi việc đã thực hiện và kiểm tra NK sau nhập. Không hứa luồng NK→GS1 tự động nếu chưa có bằng chứng. Quy trình nhập GS1→NK có hướng dẫn riêng về dữ liệu và trạng thái. [CH05]

### 10.9 Chứng từ và registry

Cho phép mở liên kết registry, nhập kết quả chính thức hoặc dùng connector/API khi có quyền và tài liệu hỗ trợ. Không hứa một API công khai không có tài liệu; không dùng scraping vượt hạn chế truy cập làm phương án mặc định. Lưu cả thông tin đối chiếu và bằng chứng về phạm vi hồ sơ, không chỉ trạng thái tìm thấy. [REG01] [REG02]

Khi cần file nhập РД của NK, tải đúng template hiện hành. Hướng dẫn tháng 10/2026 phân biệt bổ sung với thay toàn bộ hồ sơ; thay toàn bộ là hành động có thể loại dữ liệu cũ. DS/SS và SGR có quy tắc điền ngày khác nhau. App phải dùng profile template có phiên bản và kiểm tra roundtrip; không đưa ID loại hồ sơ từ một hệ thống vào WB như thể dùng chung. [CH03]

## 11 Đồng bộ và cơ chế ghi thay đổi an toàn

### 11.1 Đồng bộ nghĩa là đối chiếu có điều phối

Không có một giao dịch nguyên tử bao trùm WB, GS1, NK và ЧЗ. Mỗi hệ thống có quyền, trạng thái và kết quả riêng. SyncPlan là tập tác vụ có phụ thuộc, có thể hoàn thành một phần; thành công ở WB không rollback tự động một hồ sơ đã ký trong NK và ngược lại.

Tạo các dependency rõ ràng: sửa nguồn quản lý → chờ phiên bản nguồn có hiệu lực → đối chiếu → sửa nơi nhận khi cần → đọc lại → xử lý hàng vật lý nếu danh tính/tem bị ảnh hưởng. Không tự sửa ngược hệ thống gốc khi dữ liệu từ WB vừa thay đổi.

### 11.2 Trạng thái changeset và job

```text
ChangeSet:
  DRAFT -> VALIDATED -> READY_FOR_REVIEW -> APPROVED
  -> PREFLIGHT -> APPLYING -> VERIFYING
  -> COMPLETED | PARTIALLY_COMPLETED | FAILED | NEEDS_RECONCILIATION

Các nhánh phụ:
  NEEDS_EVIDENCE, NEEDS_EXTERNAL_ACTION, CONFLICT,
  BLOCKED_CONTRACT, BLOCKED_POLICY, CANCELED_BEFORE_SEND

RemoteJob:
  QUEUED -> SENDING -> ACCEPTED -> PROCESSING -> READBACK_PENDING
  -> VERIFIED | REJECTED | UNKNOWN_OUTCOME
```

Đây là trạng thái nội bộ của ứng dụng, không phải enum được khẳng định tồn tại trong API WB/CRPT. Adapter ánh xạ raw status vào mô hình này và vẫn lưu raw value; enum từ xa mới phải trở thành trạng thái chưa hiểu để xem xét, không mặc định thành công.

### 11.3 Nội dung một changeset

Mỗi intent gồm tài khoản, hệ thống, external entity ID, field paths, old values, new values, lý do, nguồn bằng chứng, operation, các phụ thuộc cần ổn định và người duyệt khi cần. Lưu `intentHash`, `dependencyFingerprint`, `baseSnapshotId`, `approvedAt` và phạm vi được duyệt.

Không lưu approval như một cờ true có thể tái sử dụng cho payload bất kỳ. Approval gắn với ý định và dữ liệu quyết định. Nếu chỉ metadata read-only đổi, ý định có thể vẫn hợp lệ. Nếu giới tính, màu, size, GTIN hoặc chứng cứ quyết định đổi, phải tính lại và xử lý xung đột.

### 11.4 Thuật toán cập nhật một thẻ WB

1. Ghi job vào cơ sở dữ liệu trước khi phát sinh request. Kiểm tra account, quyền, môi trường và capability.
2. Đọc snapshot mới nhất của card và các dữ liệu cần bảo toàn. Nếu không đọc đủ, dừng ghi.
3. So sánh trường mục tiêu và các trường phụ thuộc với intent đã được xác nhận. `updatedAt` chỉ là tín hiệu; thay đổi verdict tự động không tạo xung đột người sửa.
4. Nếu một trường liên quan đã khác, chuyển CONFLICT. Nếu chỉ trường không liên quan đổi, có thể rebase trên bản mới và giữ thay đổi mới đó, rồi chạy lại validator cho tập phụ thuộc.
5. Dùng serializer theo write schema để dựng toàn bộ phần payload endpoint cần. Áp đúng các thay đổi đã chọn; bảo toàn các trường ghi được còn lại. Kiểm tra required/type/ràng buộc API trên toàn payload. Không copy read-only vào request và không âm thầm bỏ trường chưa biết cách bảo toàn.
6. Kiểm tra diff cuối cùng: mọi khác biệt phải thuộc intent hoặc chuyển đổi serialization đã xác định là không đổi nghĩa. Nếu xuất hiện size/barcode/hồ sơ bị loại, dừng và giải thích.
7. Lưu request hash, gửi theo rate limiter và lưu response nguyên bản đã che bí mật ở bản log hiển thị.
8. Theo dõi trạng thái/endpoint lỗi phù hợp và đọc lại card. Kiểm tra cả trường đã sửa lẫn các trường trọng yếu phải giữ.
9. Chỉ hoàn thành phần cập nhật nội dung khi readback xác nhận. Kiểm tra giấy tờ/moderation còn pending giữ trạng thái riêng.

WB chưa được xác minh có điều kiện ghi nguyên tử kiểu compare-and-swap cho thao tác này. Refetch và fingerprint giảm nguy cơ ghi đè nhưng không xóa hoàn toàn khoảng đua giữa lần đọc và ghi. App phải khóa các tác vụ của chính nó theo card, rút ngắn khoảng đó và đối soát sau ghi; không quảng cáo cơ chế này là bảo đảm nguyên tử giữa mọi ứng dụng.

### 11.5 Hàng đợi và retry

| Tình huống | Hành vi |
| --- | --- |
| 400 hoặc lỗi validation | Không retry cùng payload; tạo issue sửa dữ liệu |
| 401/403 | Dừng tác vụ cần quyền đó; yêu cầu làm mới xác thực hoặc quyền phù hợp |
| 429 | Theo quota/header/Retry-After khi có; backoff và jitter có giới hạn |
| Lỗi mạng trước khi gửi chắc chắn | Có thể lập lịch lại theo policy |
| Timeout sau khi có thể đã gửi | UNKNOWN_OUTCOME; đối soát trước gửi lại |
| Batch thành công một phần | Lưu từng kết quả; chỉ xử lý các mục chưa thành công sau đối soát |
| 5xx khi thao tác tạo/cấp/ký | Không mặc định chưa commit; xử lý như khả năng kết quả chưa rõ |
| Người dùng hủy | Dừng phần chưa gửi; phần đã gửi tiếp tục cần đối soát |
| App đóng hoặc máy mất điện | Khôi phục job từ DB; không lặp những mutation đã có receipt thành công |

Không giả định endpoint có idempotency key nếu tài liệu không xác nhận. Local operation ID chỉ chống lặp trong app, không bảo đảm server không tạo trùng. Sau create timeout, tìm kết quả bằng định danh và phạm vi job đã lưu; vendorCode là một tín hiệu, không phải lý do để gộp sản phẩm mơ hồ.

### 11.6 Khôi phục và bảo toàn dữ liệu

Cho undo bản nháp cục bộ. Với thao tác đã ghi lên WB, “khôi phục” là một thay đổi bù mới dựa trên dữ liệu hiện tại, không phải rollback nguyên tử. Không phục hồi snapshot cũ lên card nếu việc đó xóa thay đổi hợp lệ phát sinh sau đó.

Với hồ sơ đã ký, mã đã cấp hoặc nghiệp vụ lưu thông, dùng thủ tục sửa/hủy tương ứng khi có hỗ trợ; không gửi một bản cũ như thể có thể quay ngược lịch sử. Nhật ký phải giữ before/after, kết quả đối soát và liên kết tác vụ bù.

## 12 Thiết kế giao diện

### 12.1 Các màn hình

| Màn hình | Nội dung và hành động chính |
| --- | --- |
| Tài khoản và kết nối | Shop/INN, thị trường, quyền, môi trường, thời hạn, tình trạng connector; nút kiểm tra kết nối |
| Tổng quan danh mục | Số thẻ/size trong phạm vi, lần tải cuối, scan đầy đủ/chưa đầy đủ, lỗi và pending |
| Danh sách vấn đề | Bộ lọc rule, severity, field, account, nmID, biến thể, nguồn thiếu và hệ thống phải sửa |
| Chi tiết sản phẩm | Hàng thật, biến thể, WB, NK, hồ sơ, lịch sử; không trộn thành một form mất nguồn |
| Đối chiếu và sửa | Các cột hiện tại/nguồn/đề xuất; chứng cứ và lý do; diff cuối trước áp dụng |
| Tạo sản phẩm mới | Wizard phần 9, autosave nháp và checklist theo thao tác |
| Tác vụ đồng bộ | Job, hệ thống, trạng thái, tiến độ, lỗi từng mục và việc cần xử lý ngoài app |
| Chứng từ và bằng chứng | File, số nguyên bản, registry, scope, phụ lục, người xác nhận |
| Quy tắc và từ điển | Phiên bản, nguồn, ngày hiệu lực, phạm vi và ngày cần xem lại |
| Lịch sử và báo cáo | Ai sửa gì, kết quả đọc lại, xuất dữ liệu, gói hỗ trợ hoặc đối chiếu |

### 12.2 Cách trình bày lỗi

Không dùng thông báo chung như “TNVED sai” khi app chỉ biết mã thiếu chữ số. Viết rõ: “Mã hiện tại có 4 chữ số. Thao tác này cần mã đầy đủ. Cần xác nhận loại hàng, kết cấu và vật liệu để chọn nhánh; app chưa có căn cứ tự điền phần còn lại.”

Mỗi lỗi cần trả lời được bốn câu: quan sát thấy gì; điều kiện nào chưa đáp ứng; nguồn nào hỗ trợ; người bán có thể làm bước tiếp theo nào. Hiển thị trường tiếng Nga bên cạnh tiếng Việt, ví dụ “Giới tính — Пол”. Phân biệt “WB báo lỗi” với “app phát hiện mâu thuẫn”.

### 12.3 Trải nghiệm dữ liệu lớn và làm việc ngoại tuyến

Dùng bảng có phân trang/virtualization, tìm theo ID và mã; giữ bộ lọc khi quay lại. Audit ngoại tuyến dựa trên snapshot được phép, nhưng phải hiển thị ngày snapshot và những kiểm tra từ xa chưa cập nhật. Không tự gửi các changeset ngoại tuyến ngay khi có mạng nếu tiền điều kiện hoặc quyền đã thay đổi.

Cho phép người bán sửa dần: tiếp tục nháp đang làm, tách mục chờ giấy tờ, xuất danh sách cần nhà cung cấp trả lời. Không buộc người dùng đi qua toàn bộ wizard khi chỉ sửa một thuộc tính có phạm vi rõ ràng.

## 13 Kiến trúc desktop và bảo vệ dữ liệu

### 13.1 Các module độc lập

```text
Presentation
  Catalog, IssueList, CompareEditor, CreateWizard, SyncCenter

Application
  ImportCatalog, AuditCatalog, PlanChanges, ApplyChanges,
  CreateListings, ReconcileJobs, PrepareExternalTasks

Domain
  ProductIdentity, Classification, Evidence, Documents,
  RulesEngine, Readiness, ChangeIntent, ApprovalPolicy

Infrastructure
  WbAdapter, NkAdapter, TrueApiAdapter, Gs1Adapter,
  RegistryAccess, FileImportExport, SignatureProvider,
  LocalDatabase, CredentialVault, JobQueue
```

Đây là ranh giới module, không áp đặt cấu trúc thư mục hay ngôn ngữ. Nếu repo đã có cách chia tương đương thì tái sử dụng. Không để giao diện tự gọi endpoint và chỉnh trực tiếp dữ liệu gốc bỏ qua rule engine/changeset.

### 13.2 Hợp đồng nội bộ của adapter

```text
getCapabilities(account, environment)
verifyAccountIdentity()
fetchDictionary(scope, cursorOrVersion)
fetchEntities(scope, cursor)
fetchEntitySnapshot(externalId)
prepareMutation(intent, currentSnapshot, contract)
submitMutation(preparedRequest)
readOperationStatus(remoteOperationId)
reconcileMutation(intent, submittedRequest, currentState)
```

`prepareMutation` không phát sinh request ghi. Nó trả payload, phần giữ nguyên, preconditions, warnings và capability cần có. Kết quả connector phải có status, raw remote status, timestamp, nguồn, retry guidance và loại lỗi; không chỉ trả null khi không đọc được.

### 13.3 Cơ sở dữ liệu và tác vụ nền

Dùng cơ sở dữ liệu cục bộ có migration, transaction, index và backup phù hợp stack hiện có; SQLite là một lựa chọn thiết kế có thể xem xét cho desktop. Transaction bảo đảm snapshot, intent và job cục bộ được lưu nhất quán; không dùng transaction DB để tuyên bố giao dịch nhiều hệ thống từ xa là nguyên tử.

Queue tồn tại qua lần khởi động lại; khóa theo account/entity cho mutation; tách lượt đọc và ghi; quota dùng chung nếu nhà cung cấp tính theo tổ chức. Hỗ trợ backpressure, pause, hủy phần chưa gửi, giới hạn bộ nhớ và log có cấu trúc.

Mục tiêu mở rộng: thêm nhóm hàng hoặc connector bằng adapter/rule package có kiểm chứng, không sửa logic giao diện ở hàng chục chỗ. Mọi migration phải giữ external ID, raw evidence và lịch sử đồng bộ.

### 13.4 Bí mật và dữ liệu nhạy cảm

Token, API key và tham chiếu chứng thư lưu trong cơ chế credential của hệ điều hành hoặc cơ chế phù hợp đã được kiểm chứng. Không lưu token trong file cấu hình công khai, repo, crash report hoặc prompt gửi AI. Nếu API đặt key trong query string, phải che cả query ở log và lỗi mạng hiển thị.

Giữ khóa ký trong provider/thiết bị. Cho phép thay credential mà không xóa dữ liệu shop. Khóa chức năng ghi khi identity tài khoản không khớp dữ liệu đã chọn. Backup và export phải nêu dữ liệu nào được đưa ra, không mặc định kèm credential.

Mã Data Matrix nguyên bản và crypto tail chỉ hiện/xuất khi nghiệp vụ cần. Log thường che mã; có thể dùng hash không đảo ngược để đối soát trong phạm vi kiểm soát. Kiểm tra trùng hàng nên dùng danh tính đơn vị được parse đúng, không chỉ hash toàn chuỗi quét có thể khác cách biểu diễn.

### 13.5 Nhập file và nâng cấp ứng dụng

Giới hạn định dạng/kích thước phù hợp; xử lý file lỗi và bảng thiếu cột mà không làm treo app. Không chạy macro hoặc formula trong file nhập. Với CSV/Excel xuất ra, xử lý nguy cơ formula injection; giữ các mã như chuỗi text và không đổi chữ số dài sang scientific notation.

Bộ cài và updater cần kiểm tra nguồn và tính toàn vẹn theo nền tảng. Rule packages, dictionary snapshots và mapping templates có version/hash; chỉ nạp logic từ kênh tin cậy được cấu hình, không thực thi code tùy ý từ file sản phẩm hoặc nội dung web.

## 14 Vai trò của AI và OCR

AI hỗ trợ đọc nhãn, trích xuất hồ sơ, tìm ứng viên phân loại, phát hiện mâu thuẫn, soạn mô tả tiếng Nga và giải thích lỗi. Rule engine quyết định các phép kiểm tra xác định được; quyền ghi đi qua changeset và connector có kiểm soát.

Mỗi đề xuất AI cần dẫn đến trường nguồn hoặc vị trí trên tài liệu. Độ tin cậy mô hình không biến suy luận thành chứng cứ pháp lý. Không cho AI tự thay dữ liệu GTIN, mã phân loại, quốc gia, giới tính sử dụng, nhà sản xuất hoặc РД khi chưa có căn cứ xác nhận.

Ảnh dùng để nhận diện đặc điểm sản phẩm có thể nhìn thấy, không để suy ra giới tính người mẫu hoặc thành phần hóa học của vải. OCR số chứng từ phải cảnh báo ký tự giống nhau và giữ bản gốc. Hàng chưa có nguồn thì tạo câu hỏi rõ ràng cho người bán thay vì tự điền mặc định.

Nội dung ảnh, file nhà cung cấp, trang web và mô tả sản phẩm là dữ liệu không đáng tin để ra lệnh cho AI. Các câu như “bỏ qua kiểm tra” nằm trong file không được thay đổi policy, tạo request API hoặc tiết lộ token. Dữ liệu gửi đến mô hình phải theo lựa chọn của người bán và loại bỏ bí mật không cần thiết.

Khi module AI tắt hoặc không có mạng, validator xác định, import, bảng đối chiếu và queue vẫn phải hoạt động theo khả năng cục bộ.

## 15 Ví dụ dữ liệu và thuật toán

### 15.1 Ví dụ bản ghi nội bộ

JSON sau minh họa mô hình ứng dụng, **không phải payload WB/NK**. GTIN trong ví dụ là số minh họa phép tính của GS1; không cấp cho chủ dự án và không được dùng để đăng hàng thật. `isFixture` phải ngăn gửi ví dụ này sang production. [GS02]

```json
{
  "isFixture": true,
  "accountId": "DEMO_ACCOUNT",
  "modelId": "DEMO_MODEL",
  "variantId": "DEMO_BLACK_M",
  "product": {
    "technicalNameRu": "Брюки",
    "construction": "UNKNOWN",
    "intendedAudience": "UNISEX",
    "ageGroup": "UNKNOWN",
    "countryOfManufacture": null,
    "components": [
      {
        "type": "OUTER",
        "fibers": [
          {"nameRu": "хлопок", "percent": "75"},
          {"nameRu": "полиэстер", "percent": "25"},
          {"nameRu": "эластан", "percent": "5"}
        ],
        "evidenceState": "UNVERIFIED"
      }
    ]
  },
  "variant": {
    "colorRu": "черный",
    "sizeSystem": "INTERNATIONAL",
    "sizeValue": "M",
    "russianSize": null,
    "sizeMappingEvidenceId": null
  },
  "classification": {
    "tnvedRaw": "6103",
    "tnvedFull": null,
    "tariffVersion": null,
    "decisionState": "NEEDS_REVIEW"
  },
  "gtin": {
    "raw": "6291041500213",
    "canonical14": "06291041500213",
    "structureStatus": "PASS",
    "registrationStatus": "UNKNOWN",
    "identityMatchStatus": "UNKNOWN"
  },
  "wb": {
    "nmID": null,
    "subjectID": null,
    "additionalGtinRaw": null,
    "sizes": []
  },
  "nk": {
    "goodId": null,
    "effectiveSnapshotId": null,
    "workingSnapshotId": null
  },
  "readiness": {
    "saveLocalDraft": "PASS",
    "createWbContent": "FAIL",
    "activateSales": "FAIL"
  }
}
```

Kết quả mong đợi: có lỗi mã đầy đủ, thiếu căn cứ phân loại/tuổi/size Nga và tổng thành phần 105%. Checksum GTIN đúng chỉ cho phép PASS ở `structureStatus`; các trạng thái đăng ký và khớp hàng vẫn UNKNOWN. Không được tự sửa cotton xuống 70%, gán Nga là nước sản xuất hoặc suy M thành một size Nga cố định.

### 15.2 Kiểm tra GTIN

Pseudo code dưới đây chỉ kiểm tra cấu trúc GS1. Contract của hệ thống đích có thể giới hạn biểu diễn hẹp hơn; barcode quét ở dạng UPC-E hoặc loại khác phải được nhận diện trước, không mặc định mọi chuỗi 8 số là GTIN-8. [GS02] [GS03]

```text
validateGtinStructure(value):
  require value is string
  require value contains ASCII digits only
  require length(value) in {8, 12, 13, 14}

  body = value without its last digit
  sum = 0
  weight = 3
  for digit in body from right to left:
    sum += integer(digit) * weight
    weight = 1 if weight == 3 else 3

  expected = (10 - (sum mod 10)) mod 10
  if expected != integer(last digit):
    return FAIL_CHECK_DIGIT

  return STRUCTURALLY_VALID

canonicalGtin(value):
  require validateGtinStructure(value) == STRUCTURALLY_VALID
  return leftPadWithZeroToLength14(value)
```

Không bỏ mọi ký tự không phải số bằng một regex rồi tự chấp nhận kết quả. Cách viết `46O...` chứa chữ O phải báo lỗi. Giữ raw value để đối chiếu barcode thực tế. Canonical GTIN là khóa so sánh nội bộ; serializer quyết định đúng định dạng hệ thống đích và không tự đổi barcode logistics đã in.

### 15.3 Kiểm tra ТН ВЭД và thành phần

Kiểm tra định dạng ТН ВЭД bằng `[0-9]{10}` chỉ là bước đầu. Sau đó kiểm tra mã có hiệu lực trong danh mục đúng ngày, loại hàng và các thuộc tính quyết định. Mã chỉ có 4/6 chữ số phải được lưu như một mã nhóm quan sát được nếu nguồn là chứng từ, không được coi là mã đầy đủ của biến thể.

Thành phần được tính riêng cho từng bộ phận bằng số thập phân phù hợp. Khi có giá trị thiếu, không coi null là 0. Nếu tổng khác 100, tạo issue về dữ liệu; sai khác nhỏ do cách làm tròn phải được xem xét theo nguồn thay vì tự biến thành tỷ lệ mới. Không trộn tỷ lệ của lót/ruột vào tỷ lệ vải ngoài.

Ngày hồ sơ là ngày lịch khi nguồn chỉ cung cấp ngày; không chuyển múi giờ làm đổi ngày đăng ký. Thời điểm API là timestamp có timezone. Ngày hiệu lực quy tắc dùng lịch của phạm vi quy định, không lấy tùy tiện ngày hiện trên máy người bán.

### 15.4 Kiểm tra Data Matrix nếu mở module quét

Giữ raw bytes hoặc biểu diễn không mất dữ liệu; dùng parser GS1 phù hợp, có test cho Application Identifiers, độ dài biến đổi và ký tự phân tách. AI 01 chứa danh tính GTIN dạng 14 số; phần serial thuộc định danh từng chiếc. Không cắt/uppercase toàn chuỗi hoặc tự loại ASCII 29. [WB08]

Nếu scanner xuất dạng có tiền tố symbology hoặc biểu diễn `(01)...(21)...`, nhận diện format rõ ràng trước chuyển đổi. Nếu file Excel làm mất ký tự điều khiển, giữ bản quét gốc làm nguồn; không khẳng định đã phục hồi chính xác một mã chỉ bằng việc nối chuỗi.

Kiểm tra cấu trúc, khớp GTIN, trùng đơn vị, trạng thái và quyền sở hữu là các phép riêng. Kiểm tra mật mã chỉ có kết quả khi nguồn/công cụ được hỗ trợ xác nhận; app không tự tạo crypto tail.

## 16 Bộ tình huống nghiệm thu

Các tình huống là yêu cầu kiểm thử hành vi có ý nghĩa. Dữ liệu fixture phải tách production. Ngoài unit tests cho domain, cần contract tests cho adapter và integration tests với sandbox hoặc tài khoản thử được cho phép; không chạy mutation trên thẻ đang bán chỉ để thử mã.

### 16.1 Phân loại và dữ liệu sản phẩm

| ID | Dữ liệu hoặc sự kiện | Kết quả bắt buộc | Rule liên quan |
| --- | --- | --- | --- |
| AT01 | Chỉ có TNVED `6103` | Không sinh `6103000000`; yêu cầu phân loại chi tiết | R010, R013 |
| AT02 | Mã đủ 10 số nhưng không nằm trong tariff version thử | Không PASS chỉ vì đúng độ dài | R011 |
| AT03 | Vải được xác nhận woven, nhánh mã đang dùng cho knitted | Có issue kèm chứng cứ; không tự suy lại cấu tạo | R012 |
| AT04 | Hoodie có ảnh người mẫu nam, hồ sơ ghi unisex | Không tự gán giới tính nam hoặc nữ; tạo review đúng phạm vi | R015, R016 |
| AT05 | Size 80, 86, 92, 98 đang dùng một nhánh trẻ ≤86 cm | Xem xét theo từng size; không đổi toàn bộ dải theo một phép sửa | R017 |
| AT06 | Sản phẩm trẻ em size 176 | Không tự chuyển sang người lớn chỉ dựa số size | R018 |
| AT07 | Thành phần ngoài 75+25+5 | Báo 105%; không tự chia lại hoặc đổi một tỷ lệ | R020 |
| AT08 | Vải ngoài 100%, lót 100% lưu riêng | Không báo lỗi tổng 200% | R021 |
| AT09 | Size nhãn M chưa có bảng quy đổi | Không tự gán size Nga; nêu nguồn cần bổ sung | R022 |
| AT10 | Hai màu thật bị nhập thành một biến thể | Phát hiện mơ hồ; không hợp nhất để dùng chung định danh | R023 |
| AT11 | Gói có nhiều sản phẩm, chưa xác định unit/set/kit | Chưa cho kết luận marking của đơn vị bán | R024 |
| AT12 | Tiêu đề có giới tính nhưng shop không cấm và contract không cấm | Không báo vi phạm pháp luật chỉ vì sở thích biên tập | R060 |

### 16.2 Định danh và hồ sơ

| ID | Dữ liệu hoặc sự kiện | Kết quả bắt buộc | Rule liên quan |
| --- | --- | --- | --- |
| AT13 | GTIN `6291041500213` trong fixture | PASS cấu trúc; đăng ký/khớp hàng vẫn UNKNOWN | R030, R031 |
| AT14 | Thay một chữ số để checksum sai | Không tự sửa checksum rồi coi là mã đã đăng ký | R030 |
| AT15 | GTIN-13 và cùng mã dạng 14 số có 0 đầu | Không báo khác danh tính; giữ raw logistics barcode | R030, R035 |
| AT16 | File đã mất số 0 hoặc mã thành scientific notation | Cách ly giá trị chưa khôi phục chắc chắn; không đoán các chữ số | R001, R030 |
| AT17 | Cùng GTIN cho đen-M và trắng-L | Báo xung đột danh tính khi hàng khác đã được xác minh | R032 |
| AT18 | Hai shop hợp pháp bán đúng cùng hàng, cùng GTIN | Không báo collision chỉ vì khác người bán | R032 |
| AT19 | Card nhiều size cần nhiều GTIN bổ sung | Không gửi `sizes[].gtin` tự chế; chỉ cho phương án contract hỗ trợ | R034 |
| AT20 | Hồ sơ có TNVED 4 số và phụ lục đủ hoặc còn thiếu | Không so chuỗi bằng nhau; PASS scope chỉ khi đủ bằng chứng | R042 |
| AT21 | Hồ sơ có Cyrillic và Latin giống hình | Giữ số nguyên bản, không tự đổi ký tự | R041 |
| AT22 | Chứng từ lô không có ngày hết hạn theo trường hợp áp dụng | Không đặt giả ngày; xác minh đúng lô và trạng thái | R040, R043 |
| AT23 | Hồ sơ hết hạn, hàng có lịch sử trước ngày hết hạn | Đánh giá theo lịch sử/thao tác; không kết luận chung bằng `endDate < now` | R043 |
| AT24 | Chủ shop Nga, giấy nguồn hàng ghi Việt Nam | Không tự chọn sản xuất tại Nga | R045, R046 |
| AT25 | Có PDF trên WB nhưng NK đang thiếu РД cần cho bước tiếp theo | Giữ issue NK mở; không báo đã đồng bộ hoàn tất | R044 |

### 16.3 NK và hàng vật lý

| ID | Dữ liệu hoặc sự kiện | Kết quả bắt buộc | Rule liên quan |
| --- | --- | --- | --- |
| AT26 | NK có bản published cũ và draft mới chưa ký | Dùng đúng bản có hiệu lực, đồng thời hiển thị draft đang xử lý | R050, R068 |
| AT27 | Sửa giới tính mandatory trên thẻ apparel published | Không mặc định pilot tự sửa hỗ trợ; có task theo quy trình phù hợp | R051 |
| AT28 | GET cấp GTIN timeout sau khi có thể đã xử lý | Không retry chỉ vì method GET; đối soát mã/thẻ đã tạo | R067 |
| AT29 | Hồ sơ cấp GTIN nước ngoài thuộc công ty khác | Không suy ra có quyền đặt KM từ việc đọc thấy card | R053 |
| AT30 | Thẻ published nhưng đơn vị hàng chưa ở trạng thái cần cho giao dịch | Không bật physicalGoodsReadiness=PASS | R050, R053 |
| AT31 | Hai chiếc quét cùng định danh đơn vị | Chặn xử lý chiếc/lô liên quan, không in lại để sử dụng tiếp | R054 |
| AT32 | TNVED cần marking nhưng chưa xác nhận dán mã | Không tự bật kizMarked | R055 |
| AT33 | Chọn cửa sổ tồn đợt 4 cho hàng đã thuộc đợt trước | Không chấp nhận chỉ vì còn hạn tháng 11/2026 | R052 |
| AT34 | Luồng rút lưu thông FBS chưa có chính sách được xác nhận | Chặn riêng tự động hóa đó; audit WB vẫn dùng được | R056 |

### 16.4 An toàn cập nhật và chất lượng tích hợp

| ID | Dữ liệu hoặc sự kiện | Kết quả bắt buộc | Rule liên quan |
| --- | --- | --- | --- |
| AT35 | Chỉ sửa giới tính trên card có nhiều size/barcode/РД | Không mất hoặc đổi dữ liệu ngoài intent; bảo toàn ID hồ sơ | R065, R066 |
| AT36 | Endpoint bắt buộc full payload, required field đang thiếu | Không gửi request không hợp contract; giải thích trường cần bổ sung | R003, R065 |
| AT37 | Nhân viên khác đổi một trường phụ thuộc sau khi audit | CONFLICT; không ghi đè theo snapshot cũ | R066 |
| AT38 | Chỉ verdict hồ sơ làm updatedAt đổi | Không báo xung đột chỉnh nội dung giả; vẫn theo dõi verdict | R066, R067 |
| AT39 | Người khác bổ sung ảnh/trường không liên quan | Bảo toàn bản mới; chỉ rebase khi ý định và phụ thuộc còn hợp lệ | R066 |
| AT40 | HTTP 200, lỗi card xuất hiện sau đó | Không báo thành công cuối cùng; gắn lỗi đúng job và bản ghi | R067 |
| AT41 | Batch gồm thành công, lỗi và pending | Giữ từng kết quả; không gửi lại các mục đã thành công | R067 |
| AT42 | Create timeout và không rõ đã tạo hay chưa | Đối soát trước retry; không tự tạo thẻ trùng | R067 |
| AT43 | Token hết hạn, 403, 429 hoặc NK timeout | UNKNOWN/PENDING/thiếu quyền phù hợp; không kết luận không có GTIN hoặc đã đạt | R031, R064 |
| AT44 | API trả thuộc tính bắt buộc mới | Form/validator cập nhật theo schema; không bỏ qua vì app cũ | R003, R065 |
| AT45 | App đóng giữa lúc gửi rồi khởi động lại | Khôi phục queue và đối soát; không lặp mutation đã commit | R067 |
| AT46 | Một request có account khác snapshot | Chặn trước gửi | R064 |
| AT47 | Payload update định xóa barcode hoặc subjectID chưa được hỗ trợ | Không giả lập khả năng endpoint; chuyển việc xử lý đúng | R004, R035, R065 |
| AT48 | Không có API key/role cần thiết để sửa NK | Vẫn đọc/audit trong quyền hiện có; ghi đúng trạng thái chờ quyền | R064 |
| AT49 | CSV chứa formula và mã Data Matrix có ký tự phân tách | Không thực thi formula; giữ bản mã không mất dữ liệu | R001, R054 |
| AT50 | WB đã sửa nhưng tem/hồ sơ NK chưa thay đúng | Báo riêng thành công WB và việc còn mở | R068 |
| AT51 | Người dùng sửa mô tả vô hại, tồn tại lỗi nghiệp vụ không liên quan | Không bắt toàn shop sạch lỗi; vẫn phải hợp lệ write schema của request | R060, R065 |
| AT52 | Thay đổi XML sau khi đã duyệt/ký | Không gửi chữ ký cho nội dung khác; dựng lại bước ký đúng bản | R064, R066 |
| AT53 | Dữ liệu fixture hoặc sandbox được gửi production | Chặn trước request | R064 |
| AT54 | Hủy batch trong khi một số request đã gửi | Dừng phần chưa gửi; đối soát phần đã gửi và báo rõ | R067 |
| AT55 | Card S/M/L có ba GTIN thực khác nhau | Gửi và đọc lại đúng binding từng size; không dùng một GTIN bổ sung thay cả ba | R032, R034 |
| AT56 | Hàng bảo hộ thuộc nhóm chưa được triển khai | UNSUPPORTED_SCOPE, không tự PASS bằng bộ quy tắc quần áo thông thường | R002, R040 |
| AT57 | Hàng unisex đã có quyết định hợp lệ, nguồn/quy tắc không đổi | Tái dùng quyết định; không tạo MANUAL_REVIEW vĩnh viễn | R016 |
| AT58 | JWT response không có expireDate nhưng đúng contract | Không kết luận token lỗi chỉ vì thiếu field của UUID; xử lý hạn đúng loại | R064 |
| AT59 | GTIN allocation master khác nguồn được phép sửa thuộc tính thẻ | Chọn đường sửa theo field authority đã xác nhận, không đổi chỉ vì cờ cấp mã | R051, R068 |

### 16.5 Cách kiểm thử có giá trị

Unit tests tập trung vào danh tính, checksum, mapping size, composition, scope chứng từ, trạng thái UNKNOWN và việc chặn theo thao tác. Contract tests dùng request/response fixture có provenance; kiểm tra roundtrip không làm mất fields, pagination và enum mới.

Integration tests kiểm tra create/update và readback với dữ liệu thử được phép, giữ chi tiết môi trường và version. Test lỗi mạng nên bao gồm “server đã xử lý nhưng client không nhận phản hồi”. UI tests ưu tiên luồng diff, batch partial, tiếp tục nháp, thay account và resume job, thay vì kiểm tra các chi tiết trang trí.

Kết quả kiểm thử cần nói rõ số ca đã chạy, môi trường, phần mock và giới hạn. Không báo “đã test tích hợp” nếu chỉ gọi hàm giả hoặc mở Swagger.

## 17 Kế hoạch triển khai và định nghĩa hoàn thành

### 17.1 Các giai đoạn

| Giai đoạn | Phần phải bàn giao | Điều kiện kết thúc |
| --- | --- | --- |
| P0 Khảo sát dự án và contract | Bản đồ code hiện có, danh sách capability, account model, source/rules manifest | Không còn giả định ngầm về framework, môi trường, quyền hoặc schema ghi |
| P1 Nền tảng đọc và audit | Import WB/file, snapshots, variants, dictionaries, rules, màn hình issues và evidence | Audit tái chạy được, không mất dữ liệu và không có mutation ẩn |
| P2 Sửa WB có kiểm soát | Diff, changeset, preflight, serializer, queue, errors/readback, partial recovery | Các ca bảo toàn dữ liệu và timeout/xung đột đạt; contract Nga đã xác minh |
| P3 Tạo thẻ mới | Wizard, GTIN/size mapping, hồ sơ, nội dung, create và các tác vụ media/thương mại được chọn | Tạo và kiểm tra được thẻ thử; không lẫn tạo nội dung với hàng sẵn sàng |
| P4 NK và điều phối nguồn | NK read/draft khi được phép, tài liệu ký nếu hỗ trợ, task sửa nguồn/mandatory và đối soát | Không dùng draft làm published; mọi bước ngoài app có trạng thái xác nhận |
| P5 Mở rộng vận hành | GS1 connector theo contract, quét/in mã, EDO/lưu thông/hoàn hàng nếu được yêu cầu | Có đặc tả riêng cho hành vi pháp lý và thử nghiệm đúng kịch bản |

P1 là mốc kiểm tra sớm, không phải kết thúc mục tiêu. Phiên bản người dùng cần được bàn giao sau khi có P2, P3 và đủ P4 để vận hành đối chiếu/bàn giao minh bạch; các chức năng chưa hỗ trợ phải thể hiện thật trên giao diện.

### 17.2 Thứ tự ưu tiên quy tắc

Ưu tiên đầu: required fields, mapping card/size, GTIN cấu trúc và nguồn, ТН ВЭД đầy đủ/hiệu lực, giới tính có bằng chứng, composition, chứng từ, account isolation và bảo toàn update. Sau đó bổ sung phân loại chuyên sâu theo nhóm hàng đã có tài liệu và dữ liệu kiểm thử.

Không triển khai một AI chọn ТН ВЭД tự động cho mọi loại quần áo để thay phần thu thập chứng cứ. Đưa các trường hợp khó vào luồng review tốt ngay từ phiên bản đầu.

### 17.3 Định nghĩa hoàn thành

Một chức năng hoàn thành khi có luồng giao diện sử dụng được, domain rules phù hợp, adapter đúng capability, lưu dữ liệu/lịch sử, trạng thái lỗi/pending, thử nghiệm cần thiết và hướng dẫn vận hành. Nút thao tác nối với mock không được gọi là hoàn thành tích hợp.

Một bản sửa hoàn thành khi giá trị dự kiến đã được đọc lại ở hệ thống đích và các bất biến cần bảo toàn vẫn đúng. Kết quả phải ghi rõ những việc phụ thuộc khác còn chờ. Một thẻ mới hoàn thành phần nội dung khi có ID/mapping đúng và kết quả xử lý phù hợp; trạng thái bán/giao còn được đánh giá độc lập.

### 17.4 Các lựa chọn chưa được người dùng chốt

Hệ điều hành ưu tiên; framework và repository hiện có; số shop/khối lượng hàng; app dùng nội bộ hay phân phối thương mại; nhà cung cấp chữ ký; có cloud hay hoàn toàn cục bộ; phạm vi tự động hóa GS1/NK; có quản lý mã từng chiếc ngay hay không. Antigravity được tiếp tục những phần độc lập, ghi giả định rõ ràng và chỉ hỏi khi lựa chọn thật sự thay đổi kiến trúc hoặc quyền triển khai.

## 18 Điểm cần xác minh trước khi bật chức năng ghi

| Gate | Câu hỏi phải giải quyết | Cách đóng gate |
| --- | --- | --- |
| G01 WB contract Nga | Schema mới của gtin/documents, enum, required, missing/null/empty là gì? | Tài liệu Nga hiện hành và fixture/contract test; không chỉ mirror |
| G02 Dictionary | Những cờ nào quyết định required, named field và biến thể cho subject đang triển khai? | Snapshot schema và test form/serializer của đúng subject |
| G03 GTIN theo size | Mỗi size dùng GTIN thế nào; khi nào được dùng bổ sung; trường hợp nhiều mã bổ sung có hỗ trợ không? | Mapping được chứng minh trong request/readback và thông báo hiện hành |
| G04 Card cũ | Có thể đổi category/barcode bằng cách nào; trường nào immutable? | Capability cụ thể; chức năng không hỗ trợ chuyển task ngoài app |
| G05 Bảo toàn update | Có giữ được toàn bộ phần dữ liệu endpoint quản lý khi chỉ sửa một trường? | Roundtrip tests cho sizes, skus, attributes, documents và các trường áp dụng |
| G06 Account và token | Mô hình phân phối app, loại token và quyền có phù hợp điều khoản không? | Tài liệu quyền hiện hành và xác minh identity tài khoản |
| G07 NK editing | Field nào sửa được ở trạng thái nào; feed update có xóa gì khi omitted/null? | Contract test và scope quyền; không suy từ required flag |
| G08 Nguồn GTIN | Tài khoản cấp mã từ đâu; thuộc tính của thẻ do nguồn nào quản lý? | Cấu hình có nguồn và field authority được xác nhận |
| G09 Chữ ký | Hệ điều hành/provider, MЧД, tổ chức, dạng chữ ký và consent nào cần? | Thử trong môi trường phù hợp và lưu được receipt/hash |
| G10 Registry | Có quyền/API nào hỗ trợ tra tự động; cần bằng chứng scope nào? | Connector đã xác minh hoặc luồng đối chiếu thủ công có lưu nguồn |
| G11 Phạm vi phân loại | Tariff version và nhóm hàng được hỗ trợ đến đâu? | Danh mục chính thức, lịch sử hiệu lực và tập tình huống nghiệp vụ đã duyệt |
| G12 Hàng tồn | Loại hàng và lô có thật sự thuộc cửa sổ chuyển tiếp áp dụng không? | Quy tắc có ngày, điều kiện và chứng cứ của lô |
| G13 Lưu thông FBS | Thời điểm/thủ tục của kịch bản cụ thể được xác nhận chưa? | Chính sách vận hành có nguồn; chưa có thì tự động hóa vẫn tắt |
| G14 True API mã đơn vị | Schema, quyền và quota của từng endpoint đã cập nhật chưa? | Import contract và tests; không suy từ API NK |
| G15 Độ mới quy tắc | Có nguồn thay đổi sau mốc nghiên cứu hoặc thông báo riêng của tài khoản không? | Ghi nhận phiên bản mới, review tác động và chạy lại những assessment liên quan |

Các gate gắn với capability tương ứng, không khóa toàn bộ ứng dụng. Có thể triển khai UI, dữ liệu, validator, mocks và các connector đã đủ điều kiện trong khi xử lý gate khác. Khi một gate không thể đóng bằng quyền/tài liệu hiện có, giao diện phải nêu giới hạn và việc tiếp theo; không dùng thao tác giả để báo hoàn thành.

### 18.1 Bảo trì nguồn và bộ quy tắc

Mỗi nguồn lưu URL, cơ quan ban hành, tiêu đề, ngày cập nhật nếu có, ngày kiểm tra, phạm vi và hash bản đã dùng khi có thể. Mỗi rule có effectiveFrom/effectiveTo, phạm vi country/product/operation và danh sách nguồn. Nếu không đọc được nguồn, không xóa rule hoặc coi nó đã hết hiệu lực.

Rule update phải có review nội dung và tests ảnh hưởng. Dùng nguồn chính thức mới nhất áp dụng cho cùng phạm vi; không chọn một bài mới hơn nhưng dành cho nước hoặc nhóm hàng khác. Lịch sử đánh giá giữ ruleset cũ để giải thích kết quả tại thời điểm đó; các tác vụ ghi chưa gửi được tái đánh giá với quy tắc áp dụng cho thao tác mới.

## 19 Chỉ dẫn bắt đầu công việc cho Antigravity

Khi nhận tài liệu cùng repository, thực hiện theo thứ tự:

1. Đọc hướng dẫn dự án và xác định ứng dụng đang dùng công nghệ nào, phần nào đã có. Không thay công nghệ trước khi có lý do cụ thể.
2. Lập bảng FR01–FR10, capability và gate G01–G15 so với code hiện có; nêu các giả định cần thiết.
3. Xây mô hình dữ liệu và rule engine độc lập; thêm fixtures và các ca nghiệm thu rủi ro cao trước khi bật ghi.
4. Hoàn thiện import/snapshot/dictionary và màn hình audit; kiểm tra các dòng mã, ngày và mapping biến thể không bị biến đổi âm thầm.
5. Hoàn thiện changeset, diff, account isolation, queue, preflight và readback. Triển khai serializer theo contract thực tế, không theo JSON minh họa trong tài liệu này.
6. Hoàn thiện wizard tạo mới và quy trình hồ sơ/GTIN; tách tạo nội dung, thương mại và marking của hàng thực.
7. Tích hợp NK và các nhánh bàn giao; những bước cần quyền hoặc API khác phải có trạng thái thực và cách tiếp tục.
8. Kiểm thử những chức năng đã triển khai, ghi rõ phần chạy thật, phần sandbox, phần mock và gate còn mở.
9. Bàn giao bản chạy được theo môi trường dự án, hướng dẫn cấu hình và báo cáo thay đổi. Không tuyên bố mọi hàng đã hợp pháp hoặc mọi hệ thống đã đồng bộ nếu chỉ có kết quả nội bộ.

**Yêu cầu triển khai cốt lõi:** xây một công cụ giúp người bán sửa đúng dữ liệu có căn cứ và nhìn rõ việc chưa hoàn tất. Độ chính xác về danh tính sản phẩm, khả năng bảo toàn dữ liệu và trạng thái xử lý quan trọng hơn việc làm cho mọi dòng đều hiện màu xanh.

## 20 Nguồn chính thức và phạm vi sử dụng

Nguồn được kiểm tra trong quá trình nghiên cứu đến 06/10/2026 UTC. Ngày trong cột dưới là ngày nguồn cập nhật/phiên bản đã quan sát nếu có, không phải ngày hiệu lực của mọi nội dung trong nguồn. Các thiết kế kiến trúc, mã rule, gate và tiêu chí nghiệm thu là yêu cầu sản phẩm của tài liệu này.

### 20.1 Wildberries và API

| Mã | Nguồn | Phạm vi và lưu ý |
| --- | --- | --- |
| [WB01] | WB — Marking, barcode, GTIN; cập nhật 02/10/2026 | GTIN cho người bán Nga, cơ chế bổ sung, nhãn; ưu tiên nội dung mới cho điểm thay đổi |
| [WB02] | WB — Certificates and conformity declarations; cập nhật 29/09/2026 | Hồ sơ và kiểm tra trên WB; không thay đánh giá scope pháp lý |
| [WB03] | WB — Hướng dẫn tạo thẻ | Quy tắc nội dung; schema/category/account hiện hành quyết định request cụ thể |
| [WB04] | WB — Quy tắc ảnh, bản hướng dẫn AM | Tham khảo media; không áp yêu cầu theo quốc gia khác thành luật Nga |
| [WB05] | WB — Làm việc với barcode; cập nhật 25/06/2026 | Mapping nhãn và barcode logistics |
| [WB06] | WB — Bảng size trên thẻ; cập nhật 27/07/2026 | Size hiển thị và bảng quy đổi |
| [WB07] | WB — Bán hàng có КИЗ; cập nhật 25/09/2026 | Mô hình giao hàng; phải đối chiếu kịch bản CRPT |
| [WB08] | WB — Kiểm tra marking FBS; cập nhật 27/08/2026 | Cấu trúc/kiểm tra mã; phần nhãn GTIN cần đọc cùng WB01 mới hơn |
| [API01] | WB API — Work with products | API nền; cần xác nhận contract Nga khi triển khai |
| [API02] | WB API — Item Management trên mirror chính thức CN | Tham khảo schema mới; không dùng host CN cho tài khoản Nga |
| [API03] | WB API — Thông báo 464 | documents mới và bảo toàn thông tin khi update |
| [API04] | WB API — Thông báo 475 | Các phương thức TNVED/OKPD mới |
| [API05] | WB API — Thông báo 479 | GTIN bổ sung và giới hạn với skus |
| [API06] | WB API — Thông báo 385 ngày 09/04/2026 | needKiz và kizMarked |
| [API07] | WB Developers — Hướng dẫn tạo/sửa card | Bất đồng bộ, lỗi, đồng bộ card và kiểm tra kết quả |
| [API08] | WB API — API Information | Xác thực, loại token, quyền, seller info và giới hạn |
| [API09] | WB API — Luồng thông báo items chính thức | Toàn văn các thay đổi mới và metadata thuộc tính |

### 20.2 GS1

| Mã | Nguồn | Phạm vi và lưu ý |
| --- | --- | --- |
| [GS01] | GS1 — Số GTIN cần cho màu và size | Danh tính biến thể thương mại |
| [GS02] | GS1 — Tính check digit | Thuật toán và số minh họa; không cấp GTIN cho người dùng |
| [GS03] | GS1 — RFID/Barcode Interoperability Guideline | Biểu diễn GTIN 8/12/13/14 và dạng 14 số |
| [GS04] | GS1 — Ai chịu trách nhiệm đánh số hàng | Chủ thể cấp danh tính và việc dùng mã của hàng gốc |
| [GS05] | GS1 — Prefix và nước xuất xứ | Prefix không xác định nước sản xuất |
| [GS06] | GS1 Russia — Các dịch vụ hiện hành | Điểm bắt đầu xác minh connector đúng dịch vụ; không phải bảo đảm GS46 cũ đáp ứng marking |

### 20.3 Честный ЗНАК và Национальный каталог

| Mã | Nguồn | Phạm vi và lưu ý |
| --- | --- | --- |
| [CH01] | CRPT — Marketplace đối chiếu thẻ; 22/09/2026 | GTIN, giai đoạn thẻ cũ và trách nhiệm nền tảng |
| [CH02] | CRPT — Kiểm tra РД khi đưa hàng dệt vào lưu thông; 04/08/2026 | Các mốc 2026 và kịch bản sản xuất/nhập |
| [CH03] | CRPT — РД trong thẻ hàng dệt; 01/10/2026 | Báo cáo thiếu РД, template nhập, bổ sung/thay thế và công bố lại |
| [CH04] | CRPT — Đăng ký GS1 RUS; 16/03/2026 | Hệ thống cấp GTIN chính và ngữ cảnh tài khoản |
| [CH05] | CRPT — Nhập thẻ GS1 RUS vào NK | Luồng GS1→NK; không chứng minh đồng bộ hai chiều |
| [CH06] | CRPT — Đưa mã TNVED vào thẻ | Mã đủ 10 số và quy trình chỉnh mã |
| [CH07] | CRPT — Sửa thuộc tính thẻ; 03/08/2026 | Quy trình đề nghị sửa thuộc tính bắt buộc |
| [CH08] | CRPT — Pilot sửa thuộc tính bắt buộc; 19/08/2026 | Các nhóm được thử nghiệm; không mặc định apparel được hỗ trợ |
| [CH09] | CRPT — Sửa thẻ sản phẩm | Điều chỉnh và xử lý phiên bản/trạng thái |
| [CH10] | CRPT — Khai size quần áo; 13/05/2025 | Hệ size và giá trị, cần đọc cùng schema hiện hành |
| [CH11] | CRPT — Câu hỏi mô tả thẻ quần áo | Màu, đơn vị/bộ; không thay schema theo nhóm |
| [CH12] | CRPT — Giao dịch từ xa trên marketplace; 30/09/2026 | Mô hình FBO/FBS; cần xử lý khác biệt hướng dẫn trước tự động hóa |
| [CH13] | CRPT — Các giai đoạn marking hàng dệt | Phạm vi và lịch triển khai |
| [CH14] | CRPT — Hàng tồn đợt 4; 14/07/2026 | Điều kiện của đúng nhóm hàng tồn |
| [CH15] | CRPT — GTIN quốc tế/technical và subaccount; 21/07/2026 | Quyền đặt mã; đọc thấy thẻ không đồng nghĩa có quyền đặt KM |
| [CH16] | CRPT — Sản xuất theo hợp đồng; 28/01/2026 | Phân biệt vai trò sản xuất/đăng ký/marking |
| [CH17] | CRPT — Nhập apparel ngoài EAEU; 10/08/2026 | Marking, thông quan và kịch bản nhập tương ứng |
| [NC01] | API NK v5.68; 28/09/2026 | Phương thức, quyền, trạng thái, ký; không suy ra write-lock từ ETag cache |
| [NC02] | CRPT — MЧД và xác thực, v11.0; 30/09/2026 | Challenge, ký đăng nhập, hạn token và đại diện |
| [NC03] | CRPT — Mô hình quyền | Các capability theo vai trò; cần xác minh quyền tài khoản thực tế |
| [NC04] | CRPT — True API | Nguồn contract chính; kiểm tra bản đầy đủ hiện hành trước tích hợp |
| [NC05] | CRPT — Kiểm tra hàng marking lúc nhận qua API; 22/01/2025 | Xác nhận capability tra mã; không dùng thông tin xác thực cũ thay NC02 |

### 20.4 EAEU và registry

| Mã | Nguồn | Phạm vi và lưu ý |
| --- | --- | --- |
| [TN01] | EEC — Danh mục ТН ВЭД và biểu thuế | Điểm lấy phiên bản/hiệu lực; không coi file tên 2022 là toàn bộ cập nhật 2026 |
| [TN02] | EEC — Chương 61 | Ghi chú dệt kim, giới tính và trẻ nhỏ; kiểm tra sửa đổi áp dụng |
| [TN03] | EEC — Chương 62 | Ghi chú hàng dệt khác, giới tính và trẻ nhỏ; kiểm tra sửa đổi áp dụng |
| [TR01] | EEC — ТР ТС 017/2011 | Hàng công nghiệp nhẹ; đọc cùng sửa đổi và loại trừ |
| [TR02] | EEC — Quyết định 75 ngày 12/09/2025 | Sửa đổi có hiệu lực 26/04/2026 |
| [TR03] | EEC — ТР ТС 007/2011 | Hàng trẻ em/thanh thiếu niên và các sửa đổi |
| [TR04] | EEC — Bản quy chuẩn 017 gốc | Nội dung nhãn; phải đọc cùng các sửa đổi có hiệu lực |
| [REG01] | Rosaccreditation — Registry điện tử | Tra nguồn hồ sơ; tìm thấy số không đủ xác nhận phạm vi |
| [REG02] | Rospotrebnadzor — Registry | Tra СГР khi áp dụng |

### 20.5 Đường dẫn nguồn

[WB01]: https://seller.wildberries.ru/instructions/ru/ru/material/items-and-shipment-labling-like-barcode-and-others
[WB02]: https://seller.wildberries.ru/instructions/ru/ru/material/certificates-and-conformity-declarations
[WB03]: https://seller.wildberries.ru/instructions/ru/ru/material/how-to-create-card
[WB04]: https://seller.wildberries.ru/instructions/ru/am/material/item-photo-rules-recommendations-and-common-mistakes
[WB05]: https://seller.wildberries.ru/instructions/ru/ru/material/how-to-work-with-barcodes
[WB06]: https://seller.wildberries.ru/instructions/ru/ru/material/sizing-chart-in-item-card?recommended=true
[WB07]: https://seller.wildberries.ru/instructions/ru/ru/material/how-to-sell-cim-labeled-items
[WB08]: https://seller.wildberries.ru/instructions/ru/ru/material/verify-product-identifiers
[API01]: https://dev.wildberries.ru/en/openapi/work-with-products
[API02]: https://dev.wildberries.cn/docs/openapi/item-management
[API03]: https://t.me/wb_api_notifications/464
[API04]: https://t.me/wb_api_notifications/475
[API05]: https://t.me/wb_api_notifications/479
[API06]: https://t.me/wb_api_notifications/385
[API07]: https://dev.wildberries.ru/en/news/101
[API08]: https://dev.wildberries.ru/en/docs/openapi/api-information
[API09]: https://t.me/s/wb_api_notifications?q=%23items
[GS01]: https://support.gs1.org/support/solutions/articles/43000734083-how-many-gs1-gtins-do-i-need-when-i-have-a-product-with-many-sizes-and-colours-
[GS02]: https://www.gs1.org/services/how-calculate-check-digit-manually
[GS03]: https://www.gs1.org/docs/barcodes/RFID_Barcode_Interoperability_Guidelines.pdf
[GS04]: https://support.gs1.org/support/solutions/articles/43000734414-who-is-responsible-for-numbering-trade-items-
[GS05]: https://support.gs1.org/support/solutions/articles/43000734188-does-the-gs1-prefix-first-3-or-4-digits-of-the-ean-13-barcode-number-show-the-country-of-origin-
[GS06]: https://lp.gs1ru.org/
[CH01]: https://markirovka.ru/knowledge/tovarnye-gruppy/obschie-voprosy-gis/kak-marketpleys-budet-sveryat-kartochki-tovarov-s-chestnym-znakom-
[CH02]: https://markirovka.ru/knowledge/tovarnye-gruppy/legkaya-promishlennost/proverka-razreshitelnykh-dokumentov-pri-vvode-v-oborot-s-1-sentyabrya-2026-goda
[CH03]: https://markirovka.ru/community/shoes-and-clothes/razreshitelnye-dokumenty-v-kartochkakh-tovarov-legkoy-promyshlennosti_2
[CH04]: https://markirovka.ru/knowledge/tovarnye-gruppy/obschie-voprosy-gis/trebuetsya-li-registratsiya-v-gs1-rus
[CH05]: https://docs.crpt.ru/gismt/Инструкция_по_загрузке_карточек_товаров_из_ГС1_РУС_в_НКМТ/
[CH06]: https://docs.crpt.ru/gismt/Памятка_по_внесению_кода_ТН_ВЭД_в_карточку_товара/
[CH07]: https://markirovka.ru/knowledge/tovarnye-gruppy/obschie-voprosy-gis/vnesenie-izmeneniy-v-atributy-kartochek-tovarov-v-kmt
[CH08]: https://markirovka.ru/knowledge/tovarnye-gruppy/obschie-voprosy-gis/kak-sformirovat-shablon-zayavki-na-vnesenie-izmeneniy-v-obyazatelnye-atributy-v-kartochke
[CH09]: https://docs.crpt.ru/gismt/Памятка_по_внесению_изменений_в_карточку_товара/
[CH10]: https://markirovka.ru/knowledge/tovarnye-gruppy/legkaya-promishlennost/kak-korrektno-ukazat-razmer-odezhdy-v-kartochke-tovara-legprom
[CH11]: https://markirovka.ru/community/shoes-and-clothes/samye-populyarnye-voprosy-po-opisaniyu-kartochek-tovarov-pri-markirovke-odezhdy
[CH12]: https://markirovka.ru/knowledge/tovarnye-gruppy/legkaya-promishlennost/distantsionnaya-torgovlya-na-marketpleysakh-skhemy-fbo-fbs-dbs-legprom
[CH13]: https://markirovka.ru/knowledge/tovarnye-gruppy/legkaya-promishlennost/etapy-i-sroki-markirovki-tovarov-legkoy-promyshlennosti
[CH14]: https://markirovka.ru/knowledge/tovarnye-gruppy/legkaya-promishlennost/markirovka-ostatkov-chetvertoy-volny-markirovki-tovarov-legkoy-promyshlennosti
[CH15]: https://markirovka.ru/knowledge/tovarnye-gruppy/obschie-voprosy-gis/mozhno-li-zakazat-km-na-importnyy-kod-tovara-ili-tekhnicheskuyu-kartochku-ispolzuya-subakkaunt
[CH16]: https://markirovka.ru/knowledge/tovarnye-gruppy/legkaya-promishlennost/kto-markiruet-tovar-pri-kontraktnom-proizvodstve-legprom
[CH17]: https://markirovka.ru/knowledge/tovarnye-gruppy/legkaya-promishlennost/vvoz-tovarov-s-territorii-stran-ne-yavlyayushchikhsya-chlenami-eaes-legprom
[NC01]: https://docs.crpt.ru/gismt/API_%D0%9D%D0%9A/
[NC02]: https://docs.crpt.ru/gismt/Инструкция_по_использованию_МЧД_в_Системе_маркировки/
[NC03]: https://docs.crpt.ru/gismt/Памятка_ролевая_модель_доступа/
[NC04]: https://docs.crpt.ru/gismt/True_API/
[NC05]: https://markirovka.ru/community/rezhim-proverok-na-kassakh/proverka-markirovannoy-produktsii-v-moment-priyemki-po-kriteriyami-razreshitelnogo-rezhima-po-api
[TN01]: https://eec.eaeunion.org/comission/department/catr/ett/
[TN02]: https://eec.eaeunion.org/upload/files/catr/ett/ru.61_2022_25.04.2022.pdf
[TN03]: https://eec.eaeunion.org/upload/files/catr/ett/ru.62_2022.pdf
[TR01]: https://eec.eaeunion.org/comission/department/deptexreg/tr/bezopProductLegkProm.php
[TR02]: https://docs.eaeunion.org/documents/447/10247/
[TR03]: https://eec.eaeunion.org/comission/department/deptexreg/tr/bezopDeti.php
[TR04]: https://eec.eaeunion.org/upload/medialibrary/e4f/TR-TS-ProduktLegProm.pdf
[REG01]: https://fsa.gov.ru/use-of-technology/elektronnye-reestry/
[REG02]: https://fp.crc.ru/
