function initSliderForProductSlider(sliderProductElement) {
    let slider = sliderProductElement.querySelector('.list');
    let items = Array.from(sliderProductElement.querySelectorAll('.list .item'));
    let next = sliderProductElement.querySelector('.buttons .next');
    let prev = sliderProductElement.querySelector('.buttons .prev');

    let lengthItems = items.length;
    let active = 0;
    let refreshInterval = setInterval(nextSlide, 1000); // Điều chỉnh khoảng thời gian theo nhu cầu

    // Sao chép các mục và thêm vào slider để tạo hiệu ứng vô tận
    items.forEach(item => {
        let clone = item.cloneNode(true);
        slider.appendChild(clone);
    });

    items.forEach(item => {
        let clone = item.cloneNode(true);
        slider.insertBefore(clone, items[0]);
    });

    // Cập nhật danh sách các mục
    items = Array.from(sliderProductElement.querySelectorAll('.list .item'));

    let visibleItemsCount = 14; // Số lượng mục hiển thị cùng một lúc
    // Điều chỉnh kích thước slider để chứa tất cả các mục
    slider.style.width = `${visibleItemsCount * items[0].offsetWidth}px`;

    function nextSlide() {
        active++;
        reloadSlider();
    }

    function prevSlide() {
        active--;
        reloadSlider();
    }

    function reloadSlider() {
        slider.style.transition = 'left 0.5s ease'; // Điều chỉnh hiệu ứng chuyển tiếp theo nhu cầu

        if (active >= items.length / 2) {
            slider.style.transition = 'none';
            active = 0;
            slider.style.left = -items[active].offsetLeft + 'px';
            setTimeout(() => {
                slider.style.transition = 'left 0.5s ease'; // Điều chỉnh hiệu ứng chuyển tiếp theo nhu cầu
                active++;
                slider.style.left = -items[active].offsetLeft + 'px';
            }, 20);
        } else if (active < 0) {
            slider.style.transition = 'none';
            active = items.length / 2 - 1;
            slider.style.left = -items[active].offsetLeft + 'px';
            setTimeout(() => {
                slider.style.transition = 'left 0.5s ease'; // Điều chỉnh hiệu ứng chuyển tiếp theo nhu cầu
                active--;
                slider.style.left = -items[active].offsetLeft + 'px';
            }, 20);
        } else {
            slider.style.left = -items[active].offsetLeft + 'px';
        }

        clearInterval(refreshInterval);
        refreshInterval = setInterval(nextSlide, 1000); // Điều chỉnh khoảng thời gian theo nhu cầu
    }

    next.onclick = nextSlide;
    prev.onclick = prevSlide;

    window.onresize = function(event) {
        slider.style.transition = 'none';
        slider.style.left = -items[active].offsetLeft + 'px';
    };

    // Khởi chạy slider
    reloadSlider();
}

// Khởi tạo slider cho mỗi .slider-product
document.querySelectorAll('.slider-product').forEach(initSliderForProductSlider);
