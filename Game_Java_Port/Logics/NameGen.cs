using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game_Java_Port {
    static class NameGen {

        public static Random NameRandomizer = new Random();

        public enum Vocables {
            a,
            ae,
            au,
            ai,
            ay,

            e,
            ee,
            eo,
            ei,
            eu,
            ey,

            i,
            io,
            iu,
            ie,

            o,
            oe,
            oo,
            ou,
            ouie,

            u,
            uo,
            ue,

            y,
            ye,
            ya,
            yo,
            you,

        }

        public enum NonVocables {
            b,

            bn,
            br,
            bl,

            c,

            cr,
            cn,
            cm,
            cl,

            ch,
            chr,
            chl,
            chn,

            ck,

            d,

            dw,
            dr,
            ds,

            f,

            fr,
            fl,

            g,

            gr,
            gl,
            gh,

            h,

            hn,
            ht,
            hr,
            hm,
            hs,

            j,

            k,
            kr,
            kl,
            kn,
            km,

            l,

            m,

            n,

            p,
            pr,
            pl,
            pn,
            ps,

            q,
            qr,
            ql,
            qn,
            qm,

            r,

            s,
            sw,
            sv,
            sr,
            sl,
            sn,
            sm,
            str,
            st,
            sd,
            sp,
            sh,
            shw,
            shr,
            shl,
            shn,
            shm,
            shp,

            t,
            tch,
            tr,
            ts,
            tsh,
            tw,
            tv,

            v,
            vr,
            vn,
            vl,

            w,
            wr,

            x,
            xr,

            z,
            zr,
            zw
        }

        public const int minNameLen = 2;
        public const int maxNameLen = 6;
        private const int NameRNDRange = maxNameLen-minNameLen;

        public static string RandomName { get {
                int len = NameRandomizer.Next(minNameLen, maxNameLen + 1);
                string name = "";
                for (int i = 0; i < len; i++) {
                    name += i % 2 == (len % 2) ? NonVocable : Vocable;
                }

                return char.ToUpper(name[0]) + name.Substring(1);
            } }

        public static string NonVocable {
            get {
                return getRnd(typeof(NonVocables));
            }
        }
        public static string Vocable {
            get {
                return getRnd(typeof(Vocables));
            }
        }

        private static string getRnd(Type enumType) {
            return Enum.GetNames(enumType)[NameRandomizer.Next(Enum.GetNames(enumType).Length)];
        }
    }
}
