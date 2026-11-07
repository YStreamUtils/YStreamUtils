import postcssCustomFunctions from '@csstools/postcss-custom-functions';
import postcssPresetEnv from 'postcss-preset-env';

export default {
    plugins: [
        postcssCustomFunctions(),
        postcssPresetEnv({ stage: 1 })
    ]
};
