# Yêu cầu bổ sung chức năng giấy tờ hàng hóa trên Wildberries

**Cấu hình theo shop trong phần Chi tiết đăng bài**

Phiên bản 1.0  |  Ngày 07.10.2026  |  Gửi đội phát triển phần mềm

Đề nghị bổ sung chức năng lưu thông tin giấy tờ hàng hóa theo từng shop và cập nhật hàng loạt lên các thẻ sản phẩm trên Wildberries. Người dùng chỉ cần nhập thông tin một lần trong phần mềm, chọn phạm vi áp dụng và bấm cập nhật, không phải tải bảng về, sửa từng dòng rồi tải lại.

Kết quả cần đạt là thông tin xuất hiện đúng trong mục «Документы» của thẻ WB. Chức năng phục vụ cả thẻ đã đăng và việc tự điền giấy tờ khi đăng thẻ mới.

## 1 Vị trí chức năng

**Chi tiết đăng bài** → Giấy tờ hàng hóa → Chọn shop → Cấu hình giấy tờ.

Hiển thị rõ tên shop đang thao tác. Khi đổi shop, phần mềm tải cấu hình của shop vừa chọn; dữ liệu mỗi shop được lưu độc lập.

## 2 Thông tin cần nhập

Ưu tiên triển khai phần «Декларация соответствия» đúng như ảnh cung cấp. Cùng cấu trúc cấu hình có thể quản lý thêm loại «Сертификат соответствия».

| Trường trên phần mềm | Đối chiếu WB | Yêu cầu nhập liệu |
| --- | --- | --- |
| Shop áp dụng | Tài khoản shop đang kết nối | Bắt buộc chọn đúng shop trước khi lưu hoặc cập nhật. |
| Loại giấy tờ | Декларация соответствия / Сертификат соответствия | Chọn đúng loại. Mặc định mở phần декларация theo yêu cầu. |
| Số giấy tờ | Номер | Bắt buộc. Lưu toàn bộ số dưới dạng văn bản; giữ đúng ký tự Nga và Latin. |
| Ngày bắt đầu hiệu lực | Действует от | Bắt buộc. Nhập theo DD.MM.YYYY; không tự lấy ngày đăng ký thay thế. |
| Ngày hết hiệu lực | Действует до | Bắt buộc khi không chọn không thời hạn; định dạng DD.MM.YYYY. |
| Không thời hạn | Бессрочно | Ô tích. Khi bật, không gửi ngày hết hiệu lực. |
| Tên bộ giấy tờ và phạm vi | Thông tin quản lý trong phần mềm | Dùng để chọn giấy tờ cho toàn bộ shop hoặc từng nhóm sản phẩm. |

## 3 Cách sử dụng

### Lưu cấu hình theo shop

Người dùng chọn shop, tạo bộ giấy tờ, nhập các trường ở mục 2 rồi bấm «Lưu cấu hình». Có thể sửa cấu hình và thêm nhiều giấy tờ trong cùng một bộ. Mỗi shop có thể lưu nhiều bộ để dùng cho các nhóm hàng khác nhau.

Nút «Lưu cấu hình» chỉ lưu trong phần mềm. Việc cập nhật các thẻ đã đăng được thực hiện bằng nút «Áp dụng giấy tờ lên WB».

### Cập nhật hàng loạt các thẻ đã đăng

1. Chọn shop và bộ giấy tờ muốn áp dụng.

1. Chọn «Toàn bộ thẻ của shop», «Các thẻ đang chọn» hoặc một nhóm sản phẩm đã cấu hình.

1. Bấm «Xem trước». Phần mềm tải đủ danh sách thẻ thuộc phạm vi đã chọn và hiển thị số lượng sẽ cập nhật, đã có giấy tờ và cần xử lý thêm.

1. Kiểm tra tên shop, bộ giấy tờ và phạm vi ngay trên màn hình; bấm «Áp dụng giấy tờ lên WB» để bắt đầu.

1. Theo dõi tiến độ, xem kết quả từng thẻ và bấm «Thử lại các thẻ lỗi» nếu cần.

### Tự điền khi đăng thẻ mới

Thêm tùy chọn «Tự áp dụng giấy tờ khi đăng bài». Khi bật cho một shop, phần mềm lấy bộ giấy tờ phù hợp theo cấu hình của shop đó và đưa vào quá trình đăng thẻ. Nếu WB yêu cầu tạo thẻ trước, phần mềm tự nối tiếp bước cập nhật giấy tờ sau khi có mã thẻ.

Nếu chưa có cấu hình phù hợp, hiển thị «Chưa có giấy tờ áp dụng» để người dùng bổ sung. Không hiển thị hoàn tất giấy tờ khi mới tạo được thẻ.

## 4 Quy tắc áp dụng giấy tờ

- Một bộ dùng cho toàn shop: cho phép áp dụng vào toàn bộ thẻ khi giấy tờ bao phủ các mặt hàng trong shop. «Toàn bộ» phải bao gồm tất cả các trang dữ liệu, không chỉ các thẻ đang hiển thị.

- Shop có nhiều nhóm hàng: cho phép gán bộ giấy tờ theo danh mục, mã ТН ВЭД hoặc danh sách thẻ. Hệ thống dùng phạm vi đã cấu hình để chọn bộ phù hợp; thẻ chưa được gán bộ hoặc có cấu hình xung đột phải được liệt kê để xử lý.

- Đối chiếu phạm vi sản phẩm và mã hàng: WB yêu cầu mã ТН ВЭД hoặc ОКПД2 trên thẻ phù hợp với giấy tờ. Thiếu hoặc không khớp dữ liệu có thể khiến thẻ không qua kiểm tra và bị khóa [1].

- Mặc định dùng chế độ «Bổ sung giấy tờ»: thêm giấy tờ còn thiếu và bỏ qua giấy tờ trùng. Nếu cùng loại và cùng số nhưng khác ngày hiệu lực, báo cần đối chiếu, không thêm bản trùng số. Người dùng chọn chế độ thay thế để sửa thông tin giấy tờ đã có.

- Thay đổi cấu hình chỉ có hiệu lực cho lần áp dụng tiếp theo. Lần cập nhật đang chạy dùng bản cấu hình đã chốt khi bắt đầu.

## 5 Kết quả và trạng thái xử lý

Mỗi lần cập nhật có màn hình kết quả riêng, gồm tên shop, bộ giấy tờ, thời điểm bắt đầu, tổng số thẻ trong phạm vi và tiến độ đã xử lý. Danh sách chi tiết hiển thị mã thẻ WB, mã hàng người bán, số giấy tờ, kết quả ghi dữ liệu và thông báo lỗi nếu có.

| Trạng thái trong phần mềm | Ý nghĩa và cách xử lý |
| --- | --- |
| Chờ gửi hoặc đang gửi | Thẻ đang trong hàng đợi hoặc phần mềm đang gửi cập nhật. |
| Đã ghi trên WB | Đã kiểm tra thấy đúng thông tin giấy tờ trong dữ liệu hoặc giao diện WB. |
| Đã có giấy tờ trùng | WB đã có cùng loại, số và thông tin hiệu lực; bỏ qua việc thêm lại. |
| Chưa xác nhận kết quả | Đã gửi nhưng chưa xác minh được thông tin trên WB; cần đọc lại trước khi gửi lại. |
| Cần bổ sung cấu hình | Chưa có bộ giấy tờ phù hợp hoặc có nhiều cấu hình xung đột. |
| Lỗi cập nhật | Ghi rõ thẻ lỗi và nguyên nhân, đồng thời cho phép thử lại sau khi xử lý. |

### Trạng thái kiểm tra của WB

Hiển thị riêng tình trạng «WB đang kiểm tra», «WB chấp nhận» hoặc «WB từ chối» khi đọc được phản hồi tương ứng từ WB. Nếu chưa lấy được, ghi «Chưa có thông tin kiểm tra từ WB» và cung cấp đường dẫn mở thẻ để kiểm tra.

«Đã ghi trên WB» chỉ xác nhận thao tác cập nhật dữ liệu. Phần mềm chỉ hiển thị «WB chấp nhận» khi có bằng chứng phản hồi từ WB; không suy ra trạng thái này từ việc gửi yêu cầu thành công.

Nếu WB chỉ cung cấp trạng thái tổng của thẻ, ghi rõ đây là trạng thái tổng; không suy ra tất cả giấy tờ của thẻ đã được duyệt.

## 6 Kiểm tra dữ liệu và xử lý lỗi

- Kiểm tra thiếu loại giấy tờ, số giấy tờ hoặc ngày bắt đầu; kiểm tra ngày có tồn tại và ngày kết thúc không trước ngày bắt đầu. Hiển thị lỗi ngay tại trường cần sửa.

- Giữ nguyên số giấy tờ, chỉ loại bỏ khoảng trắng thừa ở đầu và cuối. Không tự thay ký tự Nga thành Latin hoặc sửa nội dung số giấy tờ.

- Khi chọn «Không thời hạn», bỏ ngày hết hiệu lực theo cách WB hỗ trợ và xác minh ngày cũ không còn gắn với giấy tờ. Nếu ngày kết thúc đã qua, cảnh báo cần kiểm tra hiệu lực; không tự sửa ngày hoặc kết luận WB từ chối chỉ dựa vào ngày.

- Khi một thẻ lỗi, lưu kết quả riêng và tiếp tục các thẻ có thể xử lý. Khi mất quyền kết nối hoặc phiên đăng nhập, tạm dừng lượt cập nhật và yêu cầu kết nối lại đúng shop.

- Tự thử lại có giới hạn với lỗi mạng hoặc lỗi tạm thời; tuân thủ giới hạn của kênh tích hợp. Lỗi nội dung cần người dùng sửa trước khi thử lại.

- Lưu tiến độ để tiếp tục sau gián đoạn. Trước khi thử lại một yêu cầu chưa rõ kết quả, đọc lại thông tin WB để tránh thêm trùng.

## 7 Yêu cầu tích hợp cho đội phát triển

Xác minh phương thức WB hỗ trợ để ghi đầy đủ loại, số giấy tờ và thời gian hiệu lực vào đúng phần «Документы». Cần thử trên một thẻ được chọn, đối chiếu trực tiếp kết quả trên WB rồi mới triển khai xử lý hàng loạt. Chốt cách đọc trạng thái kiểm tra của WB trong cùng bước khảo sát tích hợp.

Toàn bộ thao tác cập nhật diễn ra từ phần mềm. Nếu cần bảng trung gian trong quá trình tích hợp, phần mềm tự xử lý; người dùng không phải tải bảng về và sửa thủ công.

- Gắn cấu hình và từng lượt cập nhật với đúng shop. Lưu danh sách mã thẻ đã chọn tại thời điểm bắt đầu để xác định rõ phạm vi của lượt chạy.

- Đọc dữ liệu hiện tại trước khi cập nhật. Giữ nguyên tên, mô tả, ảnh, thuộc tính, kích thước, mã vạch và các giấy tờ khác ngoài nội dung người dùng chọn thay đổi.

- Lưu lịch sử theo từng thẻ gồm giấy tờ trước và sau, thời gian và kết quả. Bấm lặp hoặc chạy lại cùng dữ liệu không tạo giấy tờ trùng.

## 8 Tiêu chí nghiệm thu

| Tình huống kiểm tra | Kết quả phải đạt |
| --- | --- |
| Hai shop có cấu hình khác nhau | Đổi shop tải đúng bộ giấy tờ; chỉ cập nhật các thẻ của shop đã chọn. |
| Nhập theo biểu mẫu trong ảnh | Loại, số, ngày bắt đầu, ngày kết thúc hoặc không thời hạn xuất hiện chính xác trên WB. |
| Áp dụng toàn bộ shop | Lấy đủ thẻ qua tất cả các trang; mỗi thẻ có kết quả riêng và tổng số khớp danh sách mục tiêu. |
| Shop có nhiều nhóm hàng | Gán đúng bộ theo phạm vi; thẻ thiếu cấu hình hoặc xung đột được báo rõ. |
| Thẻ đã có giấy tờ | Không thêm trùng; chỉ thay giấy tờ được chọn trong chế độ thay thế. |
| Thẻ có dữ liệu sản phẩm sẵn | Tên, ảnh, mô tả, size, mã vạch và thuộc tính không liên quan giữ nguyên sau cập nhật. |
| Có lỗi hoặc bị gián đoạn | Tiếp tục hoặc thử lại đúng các thẻ chưa hoàn tất; giữ được kết quả trước đó. |
| Đăng thẻ mới khi bật tự áp dụng | Lấy đúng cấu hình của shop và hoàn tất bước gắn giấy tờ; báo rõ nếu bước này lỗi. |
| Gửi thành công nhưng WB chưa duyệt | Hiển thị riêng kết quả ghi dữ liệu và tình trạng kiểm tra; không báo duyệt khi chưa có xác nhận. |

### Tài liệu đối chiếu

[1] WB Partners — [Разрешительные документы](https://seller.wildberries.ru/instructions/ru/ru/material/certificates-and-conformity-declarations). Hướng dẫn cập nhật 29.09.2026, đối chiếu ngày 07.10.2026.

Biểu mẫu đầu vào đối chiếu theo ảnh giao diện WB do người yêu cầu cung cấp: «Декларация соответствия», «Номер», «Действует от», «Действует до», «Бессрочно».
