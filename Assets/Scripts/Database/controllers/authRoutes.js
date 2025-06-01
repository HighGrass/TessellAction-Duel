const bcrypt = require('bcryptjs'); 


exports.login = async (req, res) => {
    try {
        const { email, password } = req.body;
        const user = await User.findOne({ email }); 

        if (!user || !(await bcrypt.compare(password, user.passwordHash))) {
            return res.status(401).json({ error: 'Credenciais inválidas!' });
        }

        const token = jwt.sign({ userId: user._id }, process.env.JWT_SECRET, { expiresIn: '1h' });
        res.json({
            token,
            userId: user._id,
            username: user.username,
            globalScore: user.globalScore
        });
    } catch (error) {
        res.status(500).json({ error: 'Erro no login' });
    }
};