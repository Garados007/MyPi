# MyPi

This is my version to calculate the first 100 digits of pi.

For this I have created a special structure ([BigFloat](MyPi/BigFloat.cs)) that can hold a floating
point number in any precision wanted. This structure splits the number in 100-blocks and store them
seperately (this makes it easy for me to convert it to decimal numbers afterwards). The rest is
plain old school maths.

This implementation is:

- not optimized (I think there can be a lot done)
- single threaded (some of the operations can be done multi-threaded, but lets keep it simple)
- correct (I have checked it with wolfram alpha)
- kinda slow (takes 3 seconds on my ~10 year old laptop)
