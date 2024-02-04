# Formula class
## optimizing AST based solution
## does optimizations like this `(1+2) * (a1 + 0) = 3 * a1` so that the evaluate function is optimized
### so far 28 hours
### basic strategy is to generate an unoptimized AST and then apply patterns to do local optimiztions recursively
