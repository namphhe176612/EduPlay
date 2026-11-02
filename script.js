const helloButton = document.querySelector('#hello-button');
const message = document.querySelector('#message');

helloButton.addEventListener('click', () => {
  message.textContent = 'JavaScript hoạt động rồi! Chúc bạn xây dựng một website thật hay.';
});
