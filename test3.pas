program Test3;
const
  c = 10;
  b = 20;
var
  ch: char;
begin
  case ch of
    c: writeln('C');
    b: writeln('B');
    'x' = 'y': writeln('XY');
  end;
end.