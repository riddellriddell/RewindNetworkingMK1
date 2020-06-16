using System;

namespace Sim
{
    public class FrameDataInterpilationTypeAttribute : Attribute
    {
        public enum InterpolationType
        {
            None,
            Linear,
            Bilinear,
        }

        public enum InterpolationBreakType
        {
            LessThan
        }

        public Type m_tType = null;
        public InterpolationType m_itpInterpolation;


        public FrameDataInterpilationTypeAttribute(Type tType, InterpolationType itpInterpolation = InterpolationType.Linear)
        {
            m_tType = tType;
            m_itpInterpolation = itpInterpolation;
        }
    }

    //this atribute indicates when not to interpolate and insead just use the latest data 
    public class FrameDataInterpolationBreakAttribute : Attribute
    {
        public enum DataSource
        {
            OldFrameData,
            NewFrameData,
            CustomData
        }

        public DataSource m_dscLeftDataSource;
        public string m_strLeftDataSpecifier;

        public string m_strComparitor;

        public DataSource m_dscRightDataSource;
        public string m_strRightDataSpecifier;

        public bool m_bStatementResult;

        public FrameDataInterpolationBreakAttribute(
            DataSource dscLeftDataSource,
            string strLeftDataSpecifier,
            string strComparitor,
            DataSource dscRightDataSource,
            string strRightDataSpecifier,
            bool bStatementResult)
        {
            m_dscLeftDataSource = dscLeftDataSource;
            m_strLeftDataSpecifier = strLeftDataSpecifier;

            m_strComparitor = strComparitor;

            m_dscRightDataSource = dscRightDataSource;
            m_strRightDataSpecifier = strRightDataSpecifier;

            m_bStatementResult = bStatementResult;
        }

        public string GenerateArgumentString(string strOldDataSource, string strNewDataSource)
        {
            string strArgument = "(";

            if(m_dscLeftDataSource == DataSource.OldFrameData)
            {
                strArgument += strOldDataSource + ".";
            }
            else if (m_dscLeftDataSource == DataSource.NewFrameData)
            {
                strArgument += strNewDataSource + ".";
            }

            strArgument += m_strLeftDataSpecifier + " ";
            strArgument += m_strComparitor + " ";

            if (m_dscRightDataSource == DataSource.OldFrameData)
            {
                strArgument += strOldDataSource + ".";
            }
            else if (m_dscRightDataSource == DataSource.NewFrameData)
            {
                strArgument += strNewDataSource + ".";
            }

            strArgument += m_strRightDataSpecifier;

            strArgument += ") == " + (m_bStatementResult? "true" : "false") ;

            return strArgument;
        }
    }
}