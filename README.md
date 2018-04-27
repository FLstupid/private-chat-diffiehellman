# Giới thiệu phần mềm
Đây là ứng dụng chat TCP dạng Client - Server có sử dụng mã hóa AES và phương thức trao đổi khóa Diffie Hellman bằng C#
## Các chức năng
- Trao đổi giữa client và server thông qua socket.
- Dữ liệu trao đổi giữa client và server được mã hóa bằng phương thức AES, khóa AES được trao đổi giữa client và server bằng phương thức Diffie Hellman.
## Cách sử dụng
Trong thư mục **Release** có cung cấp 2 thư mục **Client** và **Server** bên trong mỗi thư mục có file thực thi.

- Đầu tiên ta thực thi **Server** sau đó chọn IP và port để mở kết nối.

- Sau đó thực thi **Client** và điền IP / Port của Server lúc này mà ta đã điền.

# Nguyên tắc hoạt động

