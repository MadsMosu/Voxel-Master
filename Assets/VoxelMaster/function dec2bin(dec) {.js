function dec2bin(dec) {
  return (dec >>> 0).toString(2);
}

let top = z => (((z & 0b10_10_10_10) - 1) & 0b10_10_10_10) | (z & 0b01_01_01_01);
let bottom = z => (((z | 0b01_01_01_01) + 1) & 0b10_10_10_10) | (z & 0b01_01_01_01);
let left = z => (((z & 0b01_01_01_01) - 1) & 0b01_01_01_01) | (z & 0b10_10_10_10);
let right = z => (((z | 0b10_10_10_10) + 1) & 0b01_01_01_01) | (z & 0b10_10_10_10);

let test = 0b11001;
test;

let testTop = top(test);
testTop;

let testTopLeft = left(testTop);
testTopLeft;

console.log(dec2bin(left(top(test))));
